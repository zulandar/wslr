namespace Wslr.Core.Models;

/// <summary>
/// Represents the [experimental] section settings in .wslconfig.
/// </summary>
public sealed record ExperimentalSettings
{
    /// <summary>
    /// Gets a value indicating whether auto memory reclaim is enabled.
    /// Values: "disabled", "gradual", "dropcache"
    /// </summary>
    public string? AutoMemoryReclaim { get; init; }

    /// <summary>
    /// Gets a value indicating whether sparse VHD is enabled.
    /// </summary>
    public bool? SparseVhd { get; init; }

    /// <summary>
    /// Gets a value indicating whether the use of Windows DNS proxy is enabled.
    /// </summary>
    public bool? UseWindowsDnsCache { get; init; }

    /// <summary>
    /// Gets a value indicating whether best effort DNS parsing is enabled.
    /// </summary>
    public bool? BestEffortDnsParsing { get; init; }

    /// <summary>
    /// Gets the initial auto proxy timeout in milliseconds.
    /// </summary>
    public int? InitialAutoProxyTimeout { get; init; }

    /// <summary>
    /// Gets a value indicating whether host address loopback is ignored.
    /// </summary>
    public bool? IgnoredPorts { get; init; }

    /// <summary>
    /// Gets the host address loopback setting.
    /// </summary>
    public bool? HostAddressLoopback { get; init; }

    /// <summary>
    /// Gets additional settings not explicitly defined.
    /// </summary>
    public IReadOnlyDictionary<string, string> AdditionalSettings { get; init; } =
        new Dictionary<string, string>();
}
