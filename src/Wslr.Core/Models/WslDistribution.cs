namespace Wslr.Core.Models;

/// <summary>
/// Represents a WSL distribution with its current state and configuration.
/// </summary>
public sealed record WslDistribution
{
    /// <summary>
    /// Gets the name of the distribution.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the current running state of the distribution.
    /// </summary>
    public required DistributionState State { get; init; }

    /// <summary>
    /// Gets the WSL version (1 or 2) of the distribution.
    /// </summary>
    public required int Version { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is the default distribution.
    /// </summary>
    public required bool IsDefault { get; init; }
}
