namespace Wslr.Core.Models;

/// <summary>
/// Represents an available online WSL distribution that can be installed.
/// </summary>
public sealed record OnlineDistribution
{
    /// <summary>
    /// Gets the name/identifier of the distribution used for installation.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the friendly display name of the distribution.
    /// </summary>
    public required string FriendlyName { get; init; }
}
