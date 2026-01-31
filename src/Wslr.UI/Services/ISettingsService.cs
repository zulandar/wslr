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
}
