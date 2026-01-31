using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wslr.UI.Services;

namespace Wslr.UI.ViewModels;

/// <summary>
/// ViewModel for the main window.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private object? _currentViewModel;

    [ObservableProperty]
    private string _title = "WSLR - WSL Instance Manager";

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="navigationService">The navigation service.</param>
    /// <param name="distributionListViewModel">The distribution list ViewModel.</param>
    public MainWindowViewModel(
        INavigationService navigationService,
        DistributionListViewModel distributionListViewModel)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        CurrentViewModel = distributionListViewModel ?? throw new ArgumentNullException(nameof(distributionListViewModel));
    }

    /// <summary>
    /// Navigates to the distribution list view.
    /// </summary>
    [RelayCommand]
    private void NavigateToDistributions()
    {
        _navigationService.NavigateTo<DistributionListViewModel>();
    }

    /// <summary>
    /// Navigates to the settings view.
    /// </summary>
    [RelayCommand]
    private void NavigateToSettings()
    {
        // Will be implemented with SettingsViewModel
    }

    /// <summary>
    /// Minimizes the window to the system tray.
    /// </summary>
    [RelayCommand]
    private void MinimizeToTray()
    {
        _navigationService.HideMainWindow();
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
