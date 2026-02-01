using System.Text.RegularExpressions;
using Wslr.Core.Interfaces;
using Wslr.Core.Models;
using Wslr.Infrastructure.Parsing;

namespace Wslr.Infrastructure.Services;

/// <summary>
/// Implementation of <see cref="IWslDistroConfigService"/> that reads and writes wsl.conf files.
/// </summary>
public sealed partial class WslDistroConfigService : IWslDistroConfigService
{
    private const string WslConfigPath = @"\\wsl$\{0}\etc\wsl.conf";
    private const string WslConfigBackupPath = @"\\wsl$\{0}\etc\wsl.conf.backup.{1}";

    /// <inheritdoc />
    public string GetConfigPath(string distributionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distributionName);
        return string.Format(WslConfigPath, distributionName);
    }

    /// <inheritdoc />
    public bool ConfigExists(string distributionName)
    {
        var path = GetConfigPath(distributionName);
        return File.Exists(path);
    }

    /// <inheritdoc />
    public async Task<WslDistroConfig> ReadConfigAsync(string distributionName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distributionName);

        var path = GetConfigPath(distributionName);

        if (!File.Exists(path))
        {
            return new WslDistroConfig();
        }

        var content = await File.ReadAllTextAsync(path, cancellationToken);
        var document = IniDocument.Parse(content);

        return ParseFromDocument(document);
    }

    /// <inheritdoc />
    public async Task WriteConfigAsync(string distributionName, WslDistroConfig config, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distributionName);
        ArgumentNullException.ThrowIfNull(config);

        var path = GetConfigPath(distributionName);

        // Start with existing file content to preserve comments, or create new
        IniDocument document;
        if (File.Exists(path))
        {
            var existingContent = await File.ReadAllTextAsync(path, cancellationToken);
            document = IniDocument.Parse(existingContent);
        }
        else
        {
            document = new IniDocument();
        }

        MergeIntoDocument(document, config);
        var content = document.ToString();

        await File.WriteAllTextAsync(path, content, cancellationToken);
    }

    /// <inheritdoc />
    public WslDistroConfigValidationResult Validate(WslDistroConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var errors = new List<WslDistroConfigValidationError>();

        // Validate automount root path
        if (config.Automount.Root is not null)
        {
            if (!config.Automount.Root.StartsWith('/'))
            {
                errors.Add(new WslDistroConfigValidationError
                {
                    Section = "automount",
                    Key = "root",
                    Message = $"Root path must start with '/': '{config.Automount.Root}'",
                    Code = WslDistroConfigErrorCode.InvalidPath
                });
            }
        }

        // Validate automount options (basic check)
        if (config.Automount.Options is not null)
        {
            if (!IsValidMountOptions(config.Automount.Options))
            {
                errors.Add(new WslDistroConfigValidationError
                {
                    Section = "automount",
                    Key = "options",
                    Message = $"Invalid mount options format: '{config.Automount.Options}'",
                    Code = WslDistroConfigErrorCode.InvalidMountOptions
                });
            }
        }

        // Validate hostname
        if (config.Network.Hostname is not null)
        {
            if (!IsValidHostname(config.Network.Hostname))
            {
                errors.Add(new WslDistroConfigValidationError
                {
                    Section = "network",
                    Key = "hostname",
                    Message = $"Invalid hostname format: '{config.Network.Hostname}'",
                    Code = WslDistroConfigErrorCode.InvalidHostname
                });
            }
        }

        // Validate default user
        if (config.User.Default is not null)
        {
            if (!IsValidUsername(config.User.Default))
            {
                errors.Add(new WslDistroConfigValidationError
                {
                    Section = "user",
                    Key = "default",
                    Message = $"Invalid username format: '{config.User.Default}'",
                    Code = WslDistroConfigErrorCode.InvalidUsername
                });
            }
        }

        return errors.Count > 0
            ? WslDistroConfigValidationResult.Failure(errors)
            : WslDistroConfigValidationResult.Success;
    }

    /// <inheritdoc />
    public async Task<string?> CreateBackupAsync(string distributionName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distributionName);

        if (!ConfigExists(distributionName))
        {
            return null;
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = string.Format(WslConfigBackupPath, distributionName, timestamp);

        var sourcePath = GetConfigPath(distributionName);
        var content = await File.ReadAllTextAsync(sourcePath, cancellationToken);
        await File.WriteAllTextAsync(backupPath, content, cancellationToken);

        return backupPath;
    }

    private static WslDistroConfig ParseFromDocument(IniDocument document)
    {
        var automount = ParseAutomountSection(document);
        var network = ParseNetworkSection(document);
        var interop = ParseInteropSection(document);
        var user = ParseUserSection(document);
        var boot = ParseBootSection(document);

        var additionalSections = new Dictionary<string, IReadOnlyDictionary<string, string>>();
        var knownSections = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "automount", "network", "interop", "user", "boot"
        };

        foreach (var section in document.Sections)
        {
            if (!knownSections.Contains(section))
            {
                additionalSections[section] = document.GetSection(section);
            }
        }

        return new WslDistroConfig
        {
            Automount = automount,
            Network = network,
            Interop = interop,
            User = user,
            Boot = boot,
            AdditionalSections = additionalSections
        };
    }

    private static AutomountSettings ParseAutomountSection(IniDocument document)
    {
        var section = document.GetSection("automount");
        return new AutomountSettings
        {
            Enabled = ParseBool(section, "enabled"),
            Root = section.GetValueOrDefault("root"),
            Options = section.GetValueOrDefault("options"),
            MountFsTab = ParseBool(section, "mountFsTab")
        };
    }

    private static NetworkSettings ParseNetworkSection(IniDocument document)
    {
        var section = document.GetSection("network");
        return new NetworkSettings
        {
            GenerateHosts = ParseBool(section, "generateHosts"),
            GenerateResolvConf = ParseBool(section, "generateResolvConf"),
            Hostname = section.GetValueOrDefault("hostname")
        };
    }

    private static InteropSettings ParseInteropSection(IniDocument document)
    {
        var section = document.GetSection("interop");
        return new InteropSettings
        {
            Enabled = ParseBool(section, "enabled"),
            AppendWindowsPath = ParseBool(section, "appendWindowsPath")
        };
    }

    private static UserSettings ParseUserSection(IniDocument document)
    {
        var section = document.GetSection("user");
        return new UserSettings
        {
            Default = section.GetValueOrDefault("default")
        };
    }

    private static BootSettings ParseBootSection(IniDocument document)
    {
        var section = document.GetSection("boot");
        return new BootSettings
        {
            Systemd = ParseBool(section, "systemd"),
            Command = section.GetValueOrDefault("command")
        };
    }

    private static bool? ParseBool(IReadOnlyDictionary<string, string> section, string key)
    {
        if (!section.TryGetValue(key, out var value))
        {
            return null;
        }

        return value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private static void MergeIntoDocument(IniDocument document, WslDistroConfig config)
    {
        // Automount section
        MergeBool(document, "automount", "enabled", config.Automount.Enabled);
        MergeString(document, "automount", "root", config.Automount.Root);
        MergeString(document, "automount", "options", config.Automount.Options);
        MergeBool(document, "automount", "mountFsTab", config.Automount.MountFsTab);

        // Network section
        MergeBool(document, "network", "generateHosts", config.Network.GenerateHosts);
        MergeBool(document, "network", "generateResolvConf", config.Network.GenerateResolvConf);
        MergeString(document, "network", "hostname", config.Network.Hostname);

        // Interop section
        MergeBool(document, "interop", "enabled", config.Interop.Enabled);
        MergeBool(document, "interop", "appendWindowsPath", config.Interop.AppendWindowsPath);

        // User section
        MergeString(document, "user", "default", config.User.Default);

        // Boot section
        MergeBool(document, "boot", "systemd", config.Boot.Systemd);
        MergeString(document, "boot", "command", config.Boot.Command);

        // Additional sections
        foreach (var (sectionName, values) in config.AdditionalSections)
        {
            foreach (var (key, value) in values)
            {
                document.SetValue(sectionName, key, value);
            }
        }
    }

    private static void MergeBool(IniDocument document, string section, string key, bool? value)
    {
        if (value.HasValue)
        {
            document.SetValue(section, key, value.Value ? "true" : "false");
        }
        else
        {
            document.RemoveKey(section, key);
        }
    }

    private static void MergeString(IniDocument document, string section, string key, string? value)
    {
        if (value is not null)
        {
            document.SetValue(section, key, value);
        }
        else
        {
            document.RemoveKey(section, key);
        }
    }

    private static bool IsValidMountOptions(string options)
    {
        // Basic validation: should be comma-separated key=value or key pairs
        return MountOptionsRegex().IsMatch(options);
    }

    private static bool IsValidHostname(string hostname)
    {
        // Hostname: alphanumeric, hyphens allowed (not at start/end), max 63 chars
        return HostnameRegex().IsMatch(hostname);
    }

    private static bool IsValidUsername(string username)
    {
        // Linux username: lowercase, alphanumeric, underscore, hyphen, max 32 chars
        return UsernameRegex().IsMatch(username);
    }

    [GeneratedRegex(@"^[a-zA-Z0-9_,=]+$")]
    private static partial Regex MountOptionsRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?$")]
    private static partial Regex HostnameRegex();

    [GeneratedRegex(@"^[a-z_][a-z0-9_-]{0,31}$")]
    private static partial Regex UsernameRegex();
}

file static class DictionaryExtensions
{
    public static string? GetValueOrDefault(this IReadOnlyDictionary<string, string> dict, string key)
    {
        return dict.TryGetValue(key, out var value) ? value : null;
    }
}
