namespace Wslr.UI.Services;

/// <summary>
/// Service for monitoring WSL2 resource usage (CPU, memory, disk).
/// </summary>
public interface IResourceMonitorService : IDisposable
{
    /// <summary>
    /// Gets the current resource usage snapshot.
    /// </summary>
    ResourceUsage CurrentUsage { get; }

    /// <summary>
    /// Gets a value indicating whether monitoring is currently active.
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// Gets or sets the refresh interval in seconds.
    /// </summary>
    int RefreshIntervalSeconds { get; set; }

    /// <summary>
    /// Occurs when resource usage has been updated.
    /// </summary>
    event EventHandler<ResourceUsage>? ResourceUsageUpdated;

    /// <summary>
    /// Occurs when an error occurs during monitoring.
    /// </summary>
    event EventHandler<string>? MonitoringError;

    /// <summary>
    /// Starts periodic resource monitoring.
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// Stops periodic resource monitoring.
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Manually triggers a refresh of resource usage.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The current resource usage.</returns>
    Task<ResourceUsage> RefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the disk usage for a specific distribution.
    /// </summary>
    /// <param name="distributionName">The name of the distribution.</param>
    /// <returns>The disk usage in gigabytes, or null if not found.</returns>
    double? GetDistributionDiskUsage(string distributionName);
}
