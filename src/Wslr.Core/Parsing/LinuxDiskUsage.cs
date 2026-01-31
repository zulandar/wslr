namespace Wslr.Core.Parsing;

/// <summary>
/// Represents disk usage information parsed from Linux df command.
/// </summary>
public sealed record LinuxDiskUsage
{
    /// <summary>
    /// Gets the filesystem name (e.g., /dev/sdc).
    /// </summary>
    public required string Filesystem { get; init; }

    /// <summary>
    /// Gets the total size in bytes.
    /// </summary>
    public required long TotalBytes { get; init; }

    /// <summary>
    /// Gets the used space in bytes.
    /// </summary>
    public required long UsedBytes { get; init; }

    /// <summary>
    /// Gets the available space in bytes.
    /// </summary>
    public required long AvailableBytes { get; init; }

    /// <summary>
    /// Gets the mount point (e.g., /).
    /// </summary>
    public required string MountPoint { get; init; }

    /// <summary>
    /// Gets the total size in gigabytes.
    /// </summary>
    public double TotalGb => TotalBytes / 1024.0 / 1024.0 / 1024.0;

    /// <summary>
    /// Gets the used space in gigabytes.
    /// </summary>
    public double UsedGb => UsedBytes / 1024.0 / 1024.0 / 1024.0;

    /// <summary>
    /// Gets the available space in gigabytes.
    /// </summary>
    public double AvailableGb => AvailableBytes / 1024.0 / 1024.0 / 1024.0;

    /// <summary>
    /// Gets the usage percentage (0-100).
    /// </summary>
    public double UsagePercent => TotalBytes > 0 ? (double)UsedBytes / TotalBytes * 100.0 : 0;
}
