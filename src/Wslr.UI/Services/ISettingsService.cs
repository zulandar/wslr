namespace Wslr.UI.Services;

/// <summary>
/// Service for persisting user settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets a setting value.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="defaultValue">The default value if the setting doesn't exist.</param>
    /// <returns>The setting value or default.</returns>
    T Get<T>(string key, T defaultValue);

    /// <summary>
    /// Sets a setting value.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The value to save.</param>
    void Set<T>(string key, T value);

    /// <summary>
    /// Saves all pending changes to persistent storage.
    /// </summary>
    void Save();
}

/// <summary>
/// Well-known setting keys.
/// </summary>
public static class SettingKeys
{
    /// <summary>
    /// The view mode for the distribution list (Grid or List).
    /// </summary>
    public const string ViewMode = "ViewMode";

    /// <summary>
    /// Whether auto-refresh is enabled.
    /// </summary>
    public const string AutoRefreshEnabled = "AutoRefreshEnabled";

    /// <summary>
    /// The auto-refresh interval in seconds.
    /// </summary>
    public const string AutoRefreshIntervalSeconds = "AutoRefreshIntervalSeconds";

    /// <summary>
    /// Whether to minimize to tray on window close instead of exiting.
    /// </summary>
    public const string MinimizeToTrayOnClose = "MinimizeToTrayOnClose";

    /// <summary>
    /// Whether to start the application minimized to system tray.
    /// </summary>
    public const string StartMinimized = "StartMinimized";

    /// <summary>
    /// Whether to start the application when Windows starts.
    /// </summary>
    public const string StartWithWindows = "StartWithWindows";

    /// <summary>
    /// Whether to show balloon notifications for distribution state changes.
    /// </summary>
    public const string ShowNotifications = "ShowNotifications";

    /// <summary>
    /// The window width in device-independent pixels.
    /// </summary>
    public const string WindowWidth = "WindowWidth";

    /// <summary>
    /// The window height in device-independent pixels.
    /// </summary>
    public const string WindowHeight = "WindowHeight";

    /// <summary>
    /// The window left position in device-independent pixels.
    /// </summary>
    public const string WindowLeft = "WindowLeft";

    /// <summary>
    /// The window top position in device-independent pixels.
    /// </summary>
    public const string WindowTop = "WindowTop";

    /// <summary>
    /// The window state (Normal, Minimized, Maximized).
    /// </summary>
    public const string WindowState = "WindowState";

    /// <summary>
    /// Comma-separated list of pinned distribution names.
    /// </summary>
    public const string PinnedDistributions = "PinnedDistributions";

    /// <summary>
    /// Whether debug logging is enabled.
    /// </summary>
    public const string DebugLoggingEnabled = "DebugLoggingEnabled";
}
