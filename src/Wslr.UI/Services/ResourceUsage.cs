namespace Wslr.UI.Services;

/// <summary>
/// Represents the current resource usage of the WSL2 VM.
/// </summary>
public sealed record ResourceUsage
{
    /// <summary>
    /// Gets the CPU usage percentage of the WSL2 VM (0-100).
    /// </summary>
    public required double CpuUsagePercent { get; init; }

    /// <summary>
    /// Gets the memory usage in gigabytes.
    /// </summary>
    public required double MemoryUsageGb { get; init; }

    /// <summary>
    /// Gets the total disk usage across all distributions in gigabytes.
    /// </summary>
    public required double TotalDiskUsageGb { get; init; }

    /// <summary>
    /// Gets the per-distribution disk usage in gigabytes, keyed by distribution name.
    /// </summary>
    public required IReadOnlyDictionary<string, double> DiskUsageByDistribution { get; init; }

    /// <summary>
    /// Gets a value indicating whether the WSL2 VM is currently running.
    /// </summary>
    public required bool IsWslRunning { get; init; }

    /// <summary>
    /// Gets the timestamp when this usage was captured.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Creates an empty resource usage instance for when WSL is not running.
    /// </summary>
    public static ResourceUsage Empty => new()
    {
        CpuUsagePercent = 0,
        MemoryUsageGb = 0,
        TotalDiskUsageGb = 0,
        DiskUsageByDistribution = new Dictionary<string, double>(),
        IsWslRunning = false,
        Timestamp = DateTime.UtcNow
    };
}
