using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wslr.UI.Services;

namespace Wslr.UI.ViewModels;

/// <summary>
/// ViewModel for the settings view.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IStartupService _startupService;
    private readonly ILoggingService _loggingService;

    [ObservableProperty]
    private bool _minimizeToTrayOnClose;

    [ObservableProperty]
    private bool _startMinimized;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private bool _showNotifications;

    [ObservableProperty]
    private bool _autoRefreshEnabled;

    [ObservableProperty]
    private int _autoRefreshIntervalSeconds;

    [ObservableProperty]
    private bool _debugLoggingEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    /// <param name="settingsService">The settings service.</param>
    /// <param name="startupService">The startup service for Windows startup management.</param>
    /// <param name="loggingService">The logging service.</param>
    public SettingsViewModel(ISettingsService settingsService, IStartupService startupService, ILoggingService loggingService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _startupService = startupService ?? throw new ArgumentNullException(nameof(startupService));
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));

        LoadSettings();
    }

    /// <summary>
    /// Opens the log folder in the file explorer.
    /// </summary>
    [RelayCommand]
    private void OpenLogFolder()
    {
        _loggingService.OpenLogFolder();
    }

    private void LoadSettings()
    {
        MinimizeToTrayOnClose = _settingsService.Get(SettingKeys.MinimizeToTrayOnClose, true);
        StartMinimized = _settingsService.Get(SettingKeys.StartMinimized, false);
        StartWithWindows = _startupService.IsStartupEnabled();
        ShowNotifications = _settingsService.Get(SettingKeys.ShowNotifications, true);
        AutoRefreshEnabled = _settingsService.Get(SettingKeys.AutoRefreshEnabled, true);
        AutoRefreshIntervalSeconds = _settingsService.Get(SettingKeys.AutoRefreshIntervalSeconds, 5);
        DebugLoggingEnabled = _settingsService.Get(SettingKeys.DebugLoggingEnabled, false);
    }

    partial void OnMinimizeToTrayOnCloseChanged(bool value)
    {
        _settingsService.Set(SettingKeys.MinimizeToTrayOnClose, value);
        _settingsService.Save();
    }

    partial void OnStartMinimizedChanged(bool value)
    {
        _settingsService.Set(SettingKeys.StartMinimized, value);
        _settingsService.Save();
    }

    partial void OnStartWithWindowsChanged(bool value)
    {
        if (value)
        {
            _startupService.EnableStartup();
        }
        else
        {
            _startupService.DisableStartup();
        }
    }

    partial void OnShowNotificationsChanged(bool value)
    {
        _settingsService.Set(SettingKeys.ShowNotifications, value);
        _settingsService.Save();
    }

    partial void OnAutoRefreshEnabledChanged(bool value)
    {
        _settingsService.Set(SettingKeys.AutoRefreshEnabled, value);
        _settingsService.Save();
    }

    partial void OnAutoRefreshIntervalSecondsChanged(int value)
    {
        _settingsService.Set(SettingKeys.AutoRefreshIntervalSeconds, value);
        _settingsService.Save();
    }

    partial void OnDebugLoggingEnabledChanged(bool value)
    {
        _settingsService.Set(SettingKeys.DebugLoggingEnabled, value);
        _settingsService.Save();
        _loggingService.SetDebugLogging(value);
    }
}
