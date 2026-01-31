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
}
