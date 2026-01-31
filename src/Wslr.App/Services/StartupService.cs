using Microsoft.Win32;
using Wslr.UI.Services;

namespace Wslr.App.Services;

/// <summary>
/// Implementation of <see cref="IStartupService"/> using Windows Registry.
/// </summary>
public class StartupService : IStartupService
{
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "WSLR";

    private readonly string _executablePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupService"/> class.
    /// </summary>
    public StartupService()
    {
        _executablePath = Environment.ProcessPath ?? string.Empty;
    }

    /// <inheritdoc />
    public bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
            var value = key?.GetValue(AppName) as string;
            return !string.IsNullOrEmpty(value) && value.Contains("Wslr", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public void EnableStartup()
    {
        if (string.IsNullOrEmpty(_executablePath))
        {
            return;
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            key?.SetValue(AppName, $"\"{_executablePath}\"");
        }
        catch
        {
            // Ignore registry errors
        }
    }

    /// <inheritdoc />
    public void DisableStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            key?.DeleteValue(AppName, false);
        }
        catch
        {
            // Ignore registry errors
        }
    }
}
