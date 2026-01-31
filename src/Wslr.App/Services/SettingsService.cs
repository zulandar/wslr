using System.IO;
using System.Text.Json;
using Wslr.UI.Services;

namespace Wslr.App.Services;

/// <summary>
/// Implementation of <see cref="ISettingsService"/> using JSON file storage.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private readonly Dictionary<string, JsonElement> _settings;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsService"/> class.
    /// </summary>
    public SettingsService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var wslrPath = Path.Combine(appDataPath, "WSLR");
        Directory.CreateDirectory(wslrPath);
        _settingsFilePath = Path.Combine(wslrPath, "settings.json");

        _settings = LoadSettings();
    }

    /// <inheritdoc />
    public T Get<T>(string key, T defaultValue)
    {
        lock (_lock)
        {
            if (_settings.TryGetValue(key, out var element))
            {
                try
                {
                    var value = element.Deserialize<T>();
                    return value ?? defaultValue;
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }
    }

    /// <inheritdoc />
    public void Set<T>(string key, T value)
    {
        lock (_lock)
        {
            var json = JsonSerializer.SerializeToElement(value);
            _settings[key] = json;
        }
    }

    /// <inheritdoc />
    public void Save()
    {
        lock (_lock)
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_settingsFilePath, json);
            }
            catch
            {
                // Ignore save errors - settings are not critical
            }
        }
    }

    private Dictionary<string, JsonElement> LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
                    ?? new Dictionary<string, JsonElement>();
            }
        }
        catch
        {
            // Ignore load errors - start with empty settings
        }

        return new Dictionary<string, JsonElement>();
    }
}
