using System.Collections.ObjectModel;
using Wslr.Core.Interfaces;
using Wslr.Core.Models;

namespace Wslr.UI.Services;

/// <summary>
/// Implementation of <see cref="IDistributionMonitorService"/> that provides
/// centralized monitoring of WSL distribution states.
/// </summary>
public sealed class DistributionMonitorService : IDistributionMonitorService
{
    private const int MaxEventHistoryCount = 100;

    private readonly IWslService _wslService;
    private readonly ObservableCollection<WslDistribution> _distributions = [];
    private readonly ReadOnlyObservableCollection<WslDistribution> _readOnlyDistributions;
    private readonly List<MonitoringEvent> _eventHistory = [];
    private readonly object _lock = new();

    private System.Timers.Timer? _refreshTimer;
    private Dictionary<string, DistributionState> _previousStates = new();
    private bool _isRefreshing;
    private bool _isAutoRefresh;
    private bool _disposed;
    private int _refreshIntervalSeconds = 5;

    /// <inheritdoc />
    public ReadOnlyObservableCollection<WslDistribution> Distributions => _readOnlyDistributions;

    /// <inheritdoc />
    public bool IsMonitoring => _refreshTimer?.Enabled ?? false;

    /// <inheritdoc />
    public bool IsRefreshing
    {
        get
        {
            lock (_lock)
            {
                return _isRefreshing;
            }
        }
        private set
        {
            lock (_lock)
            {
                _isRefreshing = value;
            }
        }
    }

    /// <inheritdoc />
    public int RefreshIntervalSeconds
    {
        get => _refreshIntervalSeconds;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Refresh interval must be positive.");
            }

            _refreshIntervalSeconds = value;

            if (_refreshTimer is not null)
            {
                _refreshTimer.Interval = value * 1000;
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler? DistributionsRefreshed;

    /// <inheritdoc />
    public event EventHandler<DistributionStateChangedEventArgs>? DistributionStateChanged;

    /// <inheritdoc />
    public event EventHandler<string>? RefreshError;

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributionMonitorService"/> class.
    /// </summary>
    /// <param name="wslService">The WSL service.</param>
    public DistributionMonitorService(IWslService wslService)
    {
        _wslService = wslService ?? throw new ArgumentNullException(nameof(wslService));
        _readOnlyDistributions = new ReadOnlyObservableCollection<WslDistribution>(_distributions);
    }

    /// <inheritdoc />
    public void StartMonitoring()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(DistributionMonitorService));
        }

        if (_refreshTimer is not null)
        {
            return; // Already monitoring
        }

        _refreshTimer = new System.Timers.Timer(_refreshIntervalSeconds * 1000);
        _refreshTimer.Elapsed += async (_, _) =>
        {
            _isAutoRefresh = true;
            await RefreshAsync();
        };
        _refreshTimer.Start();

        AddEvent(MonitoringEvent.MonitoringStarted(_refreshIntervalSeconds));

        // Do an initial refresh (as auto-refresh)
        _isAutoRefresh = true;
        _ = RefreshAsync();
    }

    /// <inheritdoc />
    public void StopMonitoring()
    {
        if (_refreshTimer is not null)
        {
            _refreshTimer.Stop();
            _refreshTimer.Dispose();
            _refreshTimer = null;

            AddEvent(MonitoringEvent.MonitoringStopped());
        }
    }

    /// <inheritdoc />
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return;
        }

        // Prevent concurrent refreshes
        bool wasAutoRefresh;
        lock (_lock)
        {
            if (_isRefreshing)
            {
                return;
            }
            _isRefreshing = true;
            wasAutoRefresh = _isAutoRefresh;
            _isAutoRefresh = false; // Reset for next call
        }

        try
        {
            var newDistributions = await _wslService.GetDistributionsAsync(cancellationToken);

            // Build new state dictionary
            var newStates = newDistributions.ToDictionary(d => d.Name, d => d.State);

            // Detect state changes and log them
            DetectAndRaiseStateChanges(newStates);

            // Update the observable collection on the UI thread context
            UpdateDistributionCollection(newDistributions);

            // Store current states for next comparison
            _previousStates = newStates;

            // Log refresh event
            if (wasAutoRefresh)
            {
                AddEvent(MonitoringEvent.AutoRefresh(newDistributions.Count));
            }
            else
            {
                AddEvent(MonitoringEvent.ManualRefresh(newDistributions.Count));
            }

            DistributionsRefreshed?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
        }
        catch (Exception ex)
        {
            AddEvent(MonitoringEvent.Error(ex.Message));
            RefreshError?.Invoke(this, ex.Message);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<MonitoringEvent> GetEventHistory(int? maxCount = null)
    {
        lock (_lock)
        {
            // Return events in reverse order (most recent first)
            var events = _eventHistory.AsEnumerable().Reverse();

            if (maxCount.HasValue && maxCount.Value > 0)
            {
                events = events.Take(maxCount.Value);
            }

            return events.ToList();
        }
    }

    /// <inheritdoc />
    public void ClearEventHistory()
    {
        lock (_lock)
        {
            _eventHistory.Clear();
        }
    }

    private void AddEvent(MonitoringEvent monitoringEvent)
    {
        lock (_lock)
        {
            _eventHistory.Add(monitoringEvent);

            // Trim to max size
            while (_eventHistory.Count > MaxEventHistoryCount)
            {
                _eventHistory.RemoveAt(0);
            }
        }
    }

    private void DetectAndRaiseStateChanges(Dictionary<string, DistributionState> newStates)
    {
        // Check for removed distributions
        foreach (var (name, oldState) in _previousStates)
        {
            if (!newStates.ContainsKey(name))
            {
                AddEvent(MonitoringEvent.DistributionRemoved(name, oldState));
                RaiseStateChanged(name, oldState, null);
            }
        }

        // Check for added or changed distributions
        foreach (var (name, newState) in newStates)
        {
            if (_previousStates.TryGetValue(name, out var oldState))
            {
                if (oldState != newState)
                {
                    AddEvent(MonitoringEvent.StateChanged(name, oldState, newState));
                    RaiseStateChanged(name, oldState, newState);
                }
            }
            else
            {
                // New distribution
                AddEvent(MonitoringEvent.DistributionAdded(name, newState));
                RaiseStateChanged(name, null, newState);
            }
        }
    }

    private void RaiseStateChanged(string name, DistributionState? oldState, DistributionState? newState)
    {
        DistributionStateChanged?.Invoke(this, new DistributionStateChangedEventArgs
        {
            DistributionName = name,
            OldState = oldState,
            NewState = newState
        });
    }

    private void UpdateDistributionCollection(IReadOnlyList<WslDistribution> newDistributions)
    {
        var existingNames = _distributions.Select(d => d.Name).ToHashSet();
        var newNames = newDistributions.Select(d => d.Name).ToHashSet();

        // Remove distributions that no longer exist
        var toRemove = _distributions.Where(d => !newNames.Contains(d.Name)).ToList();
        foreach (var item in toRemove)
        {
            _distributions.Remove(item);
        }

        // Update or add distributions
        foreach (var distribution in newDistributions)
        {
            var existingIndex = -1;
            for (var i = 0; i < _distributions.Count; i++)
            {
                if (_distributions[i].Name == distribution.Name)
                {
                    existingIndex = i;
                    break;
                }
            }

            if (existingIndex >= 0)
            {
                // Replace with updated distribution (records are immutable)
                _distributions[existingIndex] = distribution;
            }
            else
            {
                _distributions.Add(distribution);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        StopMonitoring();
        _distributions.Clear();
        _previousStates.Clear();
        _eventHistory.Clear();
        _disposed = true;
    }
}
