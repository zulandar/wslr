using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wslr.Core.Interfaces;
using Wslr.UI.Services;

namespace Wslr.UI.ViewModels;

/// <summary>
/// ViewModel for the system tray icon context menu.
/// </summary>
public partial class TrayIconViewModel : ObservableObject
{
    private readonly IWslService _wslService;
    private readonly INavigationService _navigationService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private ObservableCollection<DistributionItemViewModel> _distributions = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="TrayIconViewModel"/> class.
    /// </summary>
    /// <param name="wslService">The WSL service.</param>
    /// <param name="navigationService">The navigation service.</param>
    /// <param name="notificationService">The notification service.</param>
    public TrayIconViewModel(
        IWslService wslService,
        INavigationService navigationService,
        INotificationService notificationService)
    {
        _wslService = wslService ?? throw new ArgumentNullException(nameof(wslService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    /// <summary>
    /// Refreshes the distribution list.
    /// </summary>
    [RelayCommand]
    private async Task RefreshDistributionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var distributions = await _wslService.GetDistributionsAsync(cancellationToken);

            Distributions.Clear();
            foreach (var distribution in distributions)
            {
                Distributions.Add(DistributionItemViewModel.FromModel(distribution));
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Error", $"Failed to refresh distributions: {ex.Message}");
        }
    }

    /// <summary>
    /// Starts a distribution.
    /// </summary>
    [RelayCommand]
    private async Task StartDistributionAsync(string distributionName, CancellationToken cancellationToken = default)
    {
        try
        {
            await _wslService.StartDistributionAsync(distributionName, cancellationToken);
            _notificationService.ShowSuccess("Started", $"{distributionName} has been started.");
            await RefreshDistributionsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Error", $"Failed to start {distributionName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Stops a distribution.
    /// </summary>
    [RelayCommand]
    private async Task StopDistributionAsync(string distributionName, CancellationToken cancellationToken = default)
    {
        try
        {
            await _wslService.TerminateDistributionAsync(distributionName, cancellationToken);
            _notificationService.ShowSuccess("Stopped", $"{distributionName} has been stopped.");
            await RefreshDistributionsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Error", $"Failed to stop {distributionName}: {ex.Message}");
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
            _notificationService.ShowSuccess("Shutdown", "All WSL distributions have been shut down.");
            await RefreshDistributionsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Error", $"Failed to shutdown WSL: {ex.Message}");
        }
    }

    /// <summary>
    /// Shows the main window.
    /// </summary>
    [RelayCommand]
    private void ShowMainWindow()
    {
        _navigationService.ShowMainWindow();
    }

    /// <summary>
    /// Exits the application.
    /// </summary>
    [RelayCommand]
    private void Exit()
    {
        _navigationService.ExitApplication();
    }
}
