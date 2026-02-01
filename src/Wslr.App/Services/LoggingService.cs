using System.Diagnostics;
using Wslr.UI.Services;

namespace Wslr.App.Services;

/// <summary>
/// Implementation of <see cref="ILoggingService"/> for controlling application logging.
/// </summary>
public class LoggingService : ILoggingService
{
    /// <inheritdoc />
    public string LogsPath => App.LogsPath;

    /// <inheritdoc />
    public void SetDebugLogging(bool enabled)
    {
        App.SetDebugLogging(enabled);
    }

    /// <inheritdoc />
    public void OpenLogFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = LogsPath,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore errors opening folder
        }
    }
}
