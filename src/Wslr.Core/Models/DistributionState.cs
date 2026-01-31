namespace Wslr.Core.Models;

/// <summary>
/// Represents the running state of a WSL distribution.
/// </summary>
public enum DistributionState
{
    /// <summary>
    /// The distribution is not running.
    /// </summary>
    Stopped,

    /// <summary>
    /// The distribution is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// The distribution is currently being installed.
    /// </summary>
    Installing,

    /// <summary>
    /// The distribution state is unknown.
    /// </summary>
    Unknown
}
