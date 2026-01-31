namespace Wslr.UI.Services;

/// <summary>
/// Service for managing application startup with Windows.
/// </summary>
public interface IStartupService
{
    /// <summary>
    /// Checks if the application is configured to start with Windows.
    /// </summary>
    /// <returns>True if startup is enabled, false otherwise.</returns>
    bool IsStartupEnabled();

    /// <summary>
    /// Enables application startup with Windows.
    /// </summary>
    void EnableStartup();

    /// <summary>
    /// Disables application startup with Windows.
    /// </summary>
    void DisableStartup();
}
