using System.Collections.ObjectModel;
using Wslr.Core.Models;

namespace Wslr.UI.Services;

/// <summary>
/// Event arguments for distribution state changes.
/// </summary>
public class DistributionStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the name of the distribution that changed.
    /// </summary>
    public required string DistributionName { get; init; }

    /// <summary>
    /// Gets the previous state of the distribution, or null if it was added.
    /// </summary>
    public DistributionState? OldState { get; init; }

    /// <summary>
    /// Gets the new state of the distribution, or null if it was removed.
    /// </summary>
    public DistributionState? NewState { get; init; }

    /// <summary>
    /// Gets a value indicating whether the distribution was added.
    /// </summary>
    public bool WasAdded => OldState is null && NewState is not null;

    /// <summary>
    /// Gets a value indicating whether the distribution was removed.
    /// </summary>
    public bool WasRemoved => OldState is not null && NewState is null;
}

/// <summary>
/// Service for monitoring WSL distribution states.
/// Provides a centralized, shared state that multiple ViewModels can subscribe to.
/// </summary>
public interface IDistributionMonitorService : IDisposable
{
    /// <summary>
    /// Gets the current list of distributions.
    /// This collection is observable and will update automatically.
    /// </summary>
    ReadOnlyObservableCollection<WslDistribution> Distributions { get; }

    /// <summary>
    /// Gets a value indicating whether monitoring is currently active.
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// Gets a value indicating whether a refresh is currently in progress.
    /// </summary>
    bool IsRefreshing { get; }

    /// <summary>
    /// Gets or sets the auto-refresh interval in seconds.
    /// </summary>
    int RefreshIntervalSeconds { get; set; }

    /// <summary>
    /// Occurs when the distribution list has been refreshed.
    /// </summary>
    event EventHandler? DistributionsRefreshed;

    /// <summary>
    /// Occurs when a distribution's state changes.
    /// </summary>
    event EventHandler<DistributionStateChangedEventArgs>? DistributionStateChanged;

    /// <summary>
    /// Occurs when an error occurs during refresh.
    /// </summary>
    event EventHandler<string>? RefreshError;

    /// <summary>
    /// Starts monitoring distribution states with auto-refresh.
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// Stops monitoring distribution states.
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Manually triggers a refresh of the distribution list.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the event history (most recent events first).
    /// </summary>
    /// <param name="maxCount">Maximum number of events to return. Defaults to all.</param>
    /// <returns>A read-only list of monitoring events.</returns>
    IReadOnlyList<MonitoringEvent> GetEventHistory(int? maxCount = null);

    /// <summary>
    /// Clears the event history.
    /// </summary>
    void ClearEventHistory();
}
