namespace Wslr.UI.Services;

/// <summary>
/// Service for fetching resource usage (CPU, memory) for individual WSL distributions.
/// </summary>
public interface IDistributionResourceService
{
    /// <summary>
    /// Gets the memory usage for a specific distribution.
    /// </summary>
    /// <param name="distributionName">The name of the distribution.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The memory usage in GB, or null if unavailable.</returns>
    Task<double?> GetMemoryUsageAsync(string distributionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the memory usage for multiple distributions in parallel.
    /// </summary>
    /// <param name="distributionNames">The names of the distributions.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A dictionary mapping distribution names to their memory usage in GB.</returns>
    Task<IReadOnlyDictionary<string, double?>> GetMemoryUsageAsync(
        IEnumerable<string> distributionNames,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the CPU usage percentage for a specific distribution.
    /// </summary>
    /// <param name="distributionName">The name of the distribution.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// The CPU usage percentage (0-100), or null if unavailable.
    /// Returns null on the first call for a distribution (no previous sample to compare).
    /// </returns>
    Task<double?> GetCpuUsageAsync(string distributionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the CPU usage for multiple distributions in parallel.
    /// </summary>
    /// <param name="distributionNames">The names of the distributions.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A dictionary mapping distribution names to their CPU usage percentage.</returns>
    Task<IReadOnlyDictionary<string, double?>> GetCpuUsageAsync(
        IEnumerable<string> distributionNames,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the CPU tracking state for a distribution.
    /// </summary>
    /// <param name="distributionName">The name of the distribution.</param>
    /// <remarks>
    /// Call this when a distribution is stopped to ensure fresh calculations when it starts again.
    /// </remarks>
    void ClearCpuState(string distributionName);

    /// <summary>
    /// Clears all CPU tracking state.
    /// </summary>
    void ClearAllCpuState();
}
