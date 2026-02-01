using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wslr.UI.Services;

namespace Wslr.UI.ViewModels;

/// <summary>
/// Navigation page indices.
/// </summary>
public enum NavigationPage
{
    /// <summary>Distributions list page.</summary>
    Distributions = 0,

    /// <summary>Terminal page.</summary>
    Terminal = 1,

    /// <summary>Install/Download page.</summary>
    Install = 2,

    /// <summary>Settings page.</summary>
    Settings = 3
}

/// <summary>
/// ViewModel for the main window.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly DistributionListViewModel _distributionListViewModel;
    private readonly TerminalViewModel _terminalViewModel;
    private readonly SettingsViewModel _settingsViewModel;

    [ObservableProperty]
    private object? _currentViewModel;

    [ObservableProperty]
    private string _title = "WSLR - WSL Instance Manager";

    [ObservableProperty]
    private int _selectedNavigationIndex;

    [ObservableProperty]
    private string _currentPageTitle = "Distributions";

    /// <summary>
    /// Gets the application version string.
    /// </summary>
    public string VersionString { get; } = GetVersionString();

    private static string GetVersionString()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        if (version is null)
        {
            return "v0.0.0";
        }

        return $"v{version.Major}.{version.Minor}.{version.Build}";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="navigationService">The navigation service.</param>
    /// <param name="distributionListViewModel">The distribution list ViewModel.</param>
    /// <param name="terminalViewModel">The terminal ViewModel.</param>
    /// <param name="settingsViewModel">The settings ViewModel.</param>
    public MainWindowViewModel(
        INavigationService navigationService,
        DistributionListViewModel distributionListViewModel,
        TerminalViewModel terminalViewModel,
        SettingsViewModel settingsViewModel)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _distributionListViewModel = distributionListViewModel ?? throw new ArgumentNullException(nameof(distributionListViewModel));
        _terminalViewModel = terminalViewModel ?? throw new ArgumentNullException(nameof(terminalViewModel));
        _settingsViewModel = settingsViewModel ?? throw new ArgumentNullException(nameof(settingsViewModel));
        CurrentViewModel = _distributionListViewModel;
        _selectedNavigationIndex = (int)NavigationPage.Distributions;
    }

    /// <summary>
    /// Navigates to the distribution list view.
    /// </summary>
    [RelayCommand]
    private void NavigateToDistributions()
    {
        SelectedNavigationIndex = (int)NavigationPage.Distributions;
        CurrentPageTitle = "Distributions";
        CurrentViewModel = _distributionListViewModel;
    }

    /// <summary>
    /// Navigates to the terminal view.
    /// </summary>
    [RelayCommand]
    private void NavigateToTerminal()
    {
        SelectedNavigationIndex = (int)NavigationPage.Terminal;
        CurrentPageTitle = "Terminal";
        CurrentViewModel = _terminalViewModel;
    }

    /// <summary>
    /// Navigates to the terminal view and opens a new tab for the specified distribution.
    /// </summary>
    /// <param name="distributionName">The distribution to connect to.</param>
    public void NavigateToTerminal(string distributionName)
    {
        NavigateToTerminal();
        _ = _terminalViewModel.OpenTabAsync(distributionName);
    }

    /// <summary>
    /// Navigates to the install/download view.
    /// </summary>
    [RelayCommand]
    private void NavigateToInstall()
    {
        SelectedNavigationIndex = (int)NavigationPage.Install;
        CurrentPageTitle = "Install";
        CurrentViewModel = new PlaceholderViewModel("Install", "Download and install new WSL distributions.");
    }

    /// <summary>
    /// Navigates to the settings view.
    /// </summary>
    [RelayCommand]
    private void NavigateToSettings()
    {
        SelectedNavigationIndex = (int)NavigationPage.Settings;
        CurrentPageTitle = "Settings";
        CurrentViewModel = _settingsViewModel;
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
