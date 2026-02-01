namespace Wslr.UI.Services;

/// <summary>
/// Service for controlling application logging.
/// </summary>
public interface ILoggingService
{
    /// <summary>
    /// Sets whether debug logging is enabled.
    /// </summary>
    /// <param name="enabled">True to enable debug logging, false for normal logging.</param>
    void SetDebugLogging(bool enabled);

    /// <summary>
    /// Opens the log folder in the file explorer.
    /// </summary>
    void OpenLogFolder();

    /// <summary>
    /// Gets the path to the logs directory.
    /// </summary>
    string LogsPath { get; }
}
