namespace Wslr.Core.Models;

/// <summary>
/// Represents the wsl.conf configuration file for a WSL distribution.
/// </summary>
public sealed record WslDistroConfig
{
    /// <summary>
    /// Gets the [automount] section settings.
    /// </summary>
    public AutomountSettings Automount { get; init; } = new();

    /// <summary>
    /// Gets the [network] section settings.
    /// </summary>
    public NetworkSettings Network { get; init; } = new();

    /// <summary>
    /// Gets the [interop] section settings.
    /// </summary>
    public InteropSettings Interop { get; init; } = new();

    /// <summary>
    /// Gets the [user] section settings.
    /// </summary>
    public UserSettings User { get; init; } = new();

    /// <summary>
    /// Gets the [boot] section settings.
    /// </summary>
    public BootSettings Boot { get; init; } = new();

    /// <summary>
    /// Gets additional sections not explicitly defined.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> AdditionalSections { get; init; } =
        new Dictionary<string, IReadOnlyDictionary<string, string>>();
}

/// <summary>
/// Represents the [automount] section in wsl.conf.
/// </summary>
public sealed record AutomountSettings
{
    /// <summary>
    /// Gets a value indicating whether Windows drives are automatically mounted.
    /// Default: true
    /// </summary>
    public bool? Enabled { get; init; }

    /// <summary>
    /// Gets the root directory where drives will be mounted.
    /// Default: /mnt/
    /// </summary>
    public string? Root { get; init; }

    /// <summary>
    /// Gets the mount options applied to Windows drives.
    /// Example: "metadata,umask=22,fmask=11"
    /// </summary>
    public string? Options { get; init; }

    /// <summary>
    /// Gets a value indicating whether /etc/fstab is processed.
    /// Default: true
    /// </summary>
    public bool? MountFsTab { get; init; }
}

/// <summary>
/// Represents the [network] section in wsl.conf.
/// </summary>
public sealed record NetworkSettings
{
    /// <summary>
    /// Gets a value indicating whether /etc/hosts is auto-generated.
    /// Default: true
    /// </summary>
    public bool? GenerateHosts { get; init; }

    /// <summary>
    /// Gets a value indicating whether /etc/resolv.conf is auto-generated.
    /// Default: true
    /// </summary>
    public bool? GenerateResolvConf { get; init; }

    /// <summary>
    /// Gets the hostname for the distribution.
    /// </summary>
    public string? Hostname { get; init; }
}

/// <summary>
/// Represents the [interop] section in wsl.conf.
/// </summary>
public sealed record InteropSettings
{
    /// <summary>
    /// Gets a value indicating whether Windows interop is enabled.
    /// Default: true
    /// </summary>
    public bool? Enabled { get; init; }

    /// <summary>
    /// Gets a value indicating whether Windows PATH is appended to $PATH.
    /// Default: true
    /// </summary>
    public bool? AppendWindowsPath { get; init; }
}

/// <summary>
/// Represents the [user] section in wsl.conf.
/// </summary>
public sealed record UserSettings
{
    /// <summary>
    /// Gets the default user when launching the distribution.
    /// </summary>
    public string? Default { get; init; }
}

/// <summary>
/// Represents the [boot] section in wsl.conf.
/// </summary>
public sealed record BootSettings
{
    /// <summary>
    /// Gets a value indicating whether systemd is enabled.
    /// Default: true (on newer WSL versions)
    /// </summary>
    public bool? Systemd { get; init; }

    /// <summary>
    /// Gets a command to run on boot.
    /// </summary>
    public string? Command { get; init; }
}
