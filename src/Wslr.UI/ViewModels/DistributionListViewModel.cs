using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wslr.Core.Interfaces;
using Wslr.UI.Services;

namespace Wslr.UI.ViewModels;

/// <summary>
/// ViewModel for the distribution list view.
/// </summary>
public partial class DistributionListViewModel : ObservableObject
{
    private readonly IWslService _wslService;
    private readonly IDialogService _dialogService;
    private System.Timers.Timer? _refreshTimer;

    [ObservableProperty]
    private ObservableCollection<DistributionItemViewModel> _distributions = [];

    [ObservableProperty]
    private DistributionItemViewModel? _selectedDistribution;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isAutoRefreshEnabled;

    [ObservableProperty]
    private int _autoRefreshIntervalSeconds = 5;

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributionListViewModel"/> class.
    /// </summary>
    /// <param name="wslService">The WSL service.</param>
    /// <param name="dialogService">The dialog service.</param>
    public DistributionListViewModel(IWslService wslService, IDialogService dialogService)
    {
        _wslService = wslService ?? throw new ArgumentNullException(nameof(wslService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    }

    partial void OnIsAutoRefreshEnabledChanged(bool value)
    {
        if (value)
        {
            StartAutoRefresh();
        }
        else
        {
            StopAutoRefresh();
        }
    }

    partial void OnAutoRefreshIntervalSecondsChanged(int value)
    {
        if (_refreshTimer is not null && IsAutoRefreshEnabled)
        {
            _refreshTimer.Interval = value * 1000;
        }
    }

    /// <summary>
    /// Loads the distribution list.
    /// </summary>
    [RelayCommand]
    private async Task LoadDistributionsAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoading)
        {
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var distributions = await _wslService.GetDistributionsAsync(cancellationToken);

            // Update existing items or add new ones
            var existingNames = Distributions.Select(d => d.Name).ToHashSet();
            var newNames = distributions.Select(d => d.Name).ToHashSet();

            // Remove distributions that no longer exist
            var toRemove = Distributions.Where(d => !newNames.Contains(d.Name)).ToList();
            foreach (var item in toRemove)
            {
                Distributions.Remove(item);
            }

            // Update or add distributions
            foreach (var distribution in distributions)
            {
                var existing = Distributions.FirstOrDefault(d => d.Name == distribution.Name);
                if (existing is not null)
                {
                    existing.UpdateFromModel(distribution);
                }
                else
                {
                    Distributions.Add(DistributionItemViewModel.FromModel(distribution));
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load distributions: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Starts the selected distribution.
    /// </summary>
    [RelayCommand]
    private async Task StartDistributionAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedDistribution is null)
        {
            return;
        }

        try
        {
            await _wslService.StartDistributionAsync(SelectedDistribution.Name, cancellationToken);
            await LoadDistributionsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to start distribution: {ex.Message}";
        }
    }

    /// <summary>
    /// Stops the selected distribution.
    /// </summary>
    [RelayCommand]
    private async Task StopDistributionAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedDistribution is null)
        {
            return;
        }

        try
        {
            await _wslService.TerminateDistributionAsync(SelectedDistribution.Name, cancellationToken);
            await LoadDistributionsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to stop distribution: {ex.Message}";
        }
    }

    /// <summary>
    /// Deletes the selected distribution.
    /// </summary>
    [RelayCommand]
    private async Task DeleteDistributionAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedDistribution is null)
        {
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Distribution",
            $"Are you sure you want to delete '{SelectedDistribution.Name}'? This action cannot be undone.");

        if (!confirmed)
        {
            return;
        }

        try
        {
            await _wslService.UnregisterDistributionAsync(SelectedDistribution.Name, cancellationToken);
            await LoadDistributionsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete distribution: {ex.Message}";
        }
    }

    /// <summary>
    /// Shuts down all WSL distributions.
    /// </summary>
    [RelayCommand]
    private async Task ShutdownAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _wslService.ShutdownAsync(cancellationToken);
            await LoadDistributionsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to shutdown WSL: {ex.Message}";
        }
    }

    private void StartAutoRefresh()
    {
        StopAutoRefresh();

        _refreshTimer = new System.Timers.Timer(AutoRefreshIntervalSeconds * 1000);
        _refreshTimer.Elapsed += async (_, _) =>
        {
            await LoadDistributionsAsync();
        };
        _refreshTimer.Start();
    }

    private void StopAutoRefresh()
    {
        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();
        _refreshTimer = null;
    }

    /// <summary>
    /// Cleans up resources.
    /// </summary>
    public void Dispose()
    {
        StopAutoRefresh();
    }
}
