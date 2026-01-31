namespace Wslr.Core.Parsing;

/// <summary>
/// Represents memory information parsed from Linux /proc/meminfo.
/// </summary>
public sealed record LinuxMemInfo
{
    /// <summary>
    /// Gets the total usable RAM in kilobytes.
    /// </summary>
    public required long MemTotalKb { get; init; }

    /// <summary>
    /// Gets the free RAM in kilobytes.
    /// </summary>
    public required long MemFreeKb { get; init; }

    /// <summary>
    /// Gets the available RAM in kilobytes (memory available for starting new applications).
    /// </summary>
    public required long MemAvailableKb { get; init; }

    /// <summary>
    /// Gets the memory used for file buffers in kilobytes.
    /// </summary>
    public required long BuffersKb { get; init; }

    /// <summary>
    /// Gets the memory used for caching in kilobytes.
    /// </summary>
    public required long CachedKb { get; init; }

    /// <summary>
    /// Gets the total swap space in kilobytes.
    /// </summary>
    public long SwapTotalKb { get; init; }

    /// <summary>
    /// Gets the free swap space in kilobytes.
    /// </summary>
    public long SwapFreeKb { get; init; }

    /// <summary>
    /// Gets the used memory in kilobytes (MemTotal - MemAvailable).
    /// </summary>
    public long UsedMemoryKb => MemTotalKb - MemAvailableKb;

    /// <summary>
    /// Gets the used memory in gigabytes.
    /// </summary>
    public double UsedMemoryGb => UsedMemoryKb / 1024.0 / 1024.0;

    /// <summary>
    /// Gets the total memory in gigabytes.
    /// </summary>
    public double TotalMemoryGb => MemTotalKb / 1024.0 / 1024.0;

    /// <summary>
    /// Gets the available memory in gigabytes.
    /// </summary>
    public double AvailableMemoryGb => MemAvailableKb / 1024.0 / 1024.0;

    /// <summary>
    /// Gets the memory usage as a percentage (0-100).
    /// </summary>
    public double UsagePercent => MemTotalKb > 0 ? (double)UsedMemoryKb / MemTotalKb * 100.0 : 0;
}
