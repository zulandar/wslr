using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wslr.Core.Interfaces;
using Wslr.Core.Models;
using Wslr.UI.Services;

namespace Wslr.UI.ViewModels;

/// <summary>
/// ViewModel for the system tray icon context menu.
/// </summary>
public partial class TrayIconViewModel : ObservableObject, IDisposable
{
    private readonly IWslService _wslService;
    private readonly INavigationService _navigationService;
    private readonly INotificationService _notificationService;
    private readonly IDistributionMonitorService _monitorService;
    private readonly IConfigurationProfileService _profileService;

    // Track actions initiated by this app to avoid duplicate notifications
    private readonly HashSet<string> _pendingActions = [];
    private readonly object _pendingActionsLock = new();

    [ObservableProperty]
    private ObservableCollection<DistributionItemViewModel> _distributions = [];

    [ObservableProperty]
    private ObservableCollection<ProfileMenuItemViewModel> _profiles = [];

    [ObservableProperty]
    private bool _isStateChangeNotificationsEnabled = true;

    [ObservableProperty]
    private string? _activeProfileName;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrayIconViewModel"/> class.
    /// </summary>
    /// <param name="wslService">The WSL service.</param>
    /// <param name="navigationService">The navigation service.</param>
    /// <param name="notificationService">The notification service.</param>
    /// <param name="monitorService">The distribution monitor service.</param>
    /// <param name="profileService">The configuration profile service.</param>
    public TrayIconViewModel(
        IWslService wslService,
        INavigationService navigationService,
        INotificationService notificationService,
        IDistributionMonitorService monitorService,
        IConfigurationProfileService profileService)
    {
        _wslService = wslService ?? throw new ArgumentNullException(nameof(wslService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _monitorService = monitorService ?? throw new ArgumentNullException(nameof(monitorService));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));

        // Subscribe to monitor service events
        _monitorService.DistributionsRefreshed += OnDistributionsRefreshed;
        _monitorService.DistributionStateChanged += OnDistributionStateChanged;
        _monitorService.RefreshError += OnRefreshError;

        // Subscribe to profile change events
        _profileService.ActiveProfileChanged += OnActiveProfileChanged;

        // Initial profile load
        _ = LoadProfilesAsync();
    }

    private void OnActiveProfileChanged(object? sender, string? profileId)
    {
        _ = LoadProfilesAsync();
    }

    private async Task LoadProfilesAsync()
    {
        try
        {
            var profiles = await _profileService.GetAllProfilesAsync();
            var activeId = _profileService.GetActiveProfileId();

            Profiles.Clear();
            foreach (var profile in profiles)
            {
                var isActive = profile.Id == activeId;
                Profiles.Add(new ProfileMenuItemViewModel
                {
                    Id = profile.Id,
                    Name = profile.Name,
                    IsActive = isActive
                });

                if (isActive)
                {
                    ActiveProfileName = profile.Name;
                }
            }

            if (activeId is null)
            {
                ActiveProfileName = null;
            }
        }
        catch
        {
            // Ignore errors loading profiles for tray
        }
    }

    private void OnDistributionsRefreshed(object? sender, EventArgs e)
    {
        UpdateDistributionsFromMonitor();
    }

    private void OnDistributionStateChanged(object? sender, DistributionStateChangedEventArgs e)
    {
        if (!IsStateChangeNotificationsEnabled)
        {
            return;
        }

        // Check if this was an action we initiated
        lock (_pendingActionsLock)
        {
            if (_pendingActions.Remove(e.DistributionName))
            {
                // This was our action, notification already shown
                return;
            }
        }

        // External state change - show notification
        if (e.WasAdded)
        {
            _notificationService.ShowInfo("Distribution Added", $"{e.DistributionName} has been installed.");
        }
        else if (e.WasRemoved)
        {
            _notificationService.ShowInfo("Distribution Removed", $"{e.DistributionName} has been unregistered.");
        }
        else if (e.OldState == DistributionState.Stopped && e.NewState == DistributionState.Running)
        {
            _notificationService.ShowInfo("Distribution Started", $"{e.DistributionName} is now running.");
        }
        else if (e.OldState == DistributionState.Running && e.NewState == DistributionState.Stopped)
        {
            _notificationService.ShowInfo("Distribution Stopped", $"{e.DistributionName} has stopped.");
        }
    }

    private void OnRefreshError(object? sender, string errorMessage)
    {
        _notificationService.ShowError("Error", $"Failed to refresh distributions: {errorMessage}");
    }

    private void UpdateDistributionsFromMonitor()
    {
        var monitorDistributions = _monitorService.Distributions;

        Distributions.Clear();
        foreach (var distribution in monitorDistributions)
        {
            Distributions.Add(DistributionItemViewModel.FromModel(distribution));
        }
    }

    private void TrackPendingAction(string distributionName)
    {
        lock (_pendingActionsLock)
        {
            _pendingActions.Add(distributionName);
        }
    }

    /// <summary>
    /// Refreshes the distribution list.
    /// </summary>
    [RelayCommand]
    private async Task RefreshDistributionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _monitorService.RefreshAsync(cancellationToken);
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
            TrackPendingAction(distributionName);
            await _wslService.StartDistributionAsync(distributionName, cancellationToken);
            _notificationService.ShowSuccess("Started", $"{distributionName} has been started.");
            await _monitorService.RefreshAsync(cancellationToken);
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
            TrackPendingAction(distributionName);
            await _wslService.TerminateDistributionAsync(distributionName, cancellationToken);
            _notificationService.ShowSuccess("Stopped", $"{distributionName} has been stopped.");
            await _monitorService.RefreshAsync(cancellationToken);
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
            // Track all currently running distributions
            foreach (var dist in Distributions.Where(d => d.IsRunning))
            {
                TrackPendingAction(dist.Name);
            }

            await _wslService.ShutdownAsync(cancellationToken);
            _notificationService.ShowSuccess("Shutdown", "All WSL distributions have been shut down.");
            await _monitorService.RefreshAsync(cancellationToken);
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

    /// <summary>
    /// Switches to a configuration profile.
    /// </summary>
    [RelayCommand]
    private async Task SwitchProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await _profileService.GetProfileAsync(profileId, cancellationToken);
            if (profile is null)
            {
                _notificationService.ShowError("Error", "Profile not found.");
                return;
            }

            var result = await _profileService.SwitchToProfileAsync(profileId, cancellationToken);

            if (result.Success)
            {
                _notificationService.ShowSuccess("Profile Switched",
                    $"Switched to '{profile.Name}'. Restart WSL for changes to take effect.");
                await LoadProfilesAsync();
            }
            else
            {
                _notificationService.ShowError("Error", $"Failed to switch profile: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Error", $"Failed to switch profile: {ex.Message}");
        }
    }

    /// <summary>
    /// Cleans up resources.
    /// </summary>
    public void Dispose()
    {
        _monitorService.DistributionsRefreshed -= OnDistributionsRefreshed;
        _monitorService.DistributionStateChanged -= OnDistributionStateChanged;
        _monitorService.RefreshError -= OnRefreshError;
        _profileService.ActiveProfileChanged -= OnActiveProfileChanged;
    }
}

/// <summary>
/// ViewModel for a profile menu item in the tray.
/// </summary>
public partial class ProfileMenuItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isActive;
}
