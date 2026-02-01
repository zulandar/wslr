namespace Wslr.Core.Interfaces;

/// <summary>
/// Factory service for creating and managing terminal sessions.
/// </summary>
public interface ITerminalSessionService
{
    /// <summary>
    /// Creates a new terminal session connected to the specified WSL distribution.
    /// </summary>
    /// <param name="distributionName">The name of the WSL distribution to connect to.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, containing the new terminal session.</returns>
    Task<ITerminalSession> CreateSessionAsync(string distributionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all currently active terminal sessions.
    /// </summary>
    IReadOnlyList<ITerminalSession> ActiveSessions { get; }

    /// <summary>
    /// Terminates all active terminal sessions.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task TerminateAllAsync();
}
