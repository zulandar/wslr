namespace Wslr.Core.Interfaces;

/// <summary>
/// Represents an active terminal session connected to a WSL distribution.
/// </summary>
public interface ITerminalSession : IAsyncDisposable
{
    /// <summary>
    /// Gets the unique identifier for this session.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the name of the WSL distribution this session is connected to.
    /// </summary>
    string DistributionName { get; }

    /// <summary>
    /// Gets a value indicating whether the session is still running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Raised when output is received from the terminal.
    /// </summary>
    event Action<string>? OutputReceived;

    /// <summary>
    /// Raised when the terminal process exits.
    /// </summary>
    event Action<int>? Exited;

    /// <summary>
    /// Writes input to the terminal's stdin.
    /// </summary>
    /// <param name="input">The input text to write.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task WriteAsync(string input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies the terminal of a resize event.
    /// </summary>
    /// <param name="columns">The new number of columns.</param>
    /// <param name="rows">The new number of rows.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Note: With the simple wsl.exe process approach, resize signals may not
    /// be properly propagated. This is a known limitation that could be addressed
    /// by using ConPTY in the future.
    /// </remarks>
    Task ResizeAsync(int columns, int rows, CancellationToken cancellationToken = default);

    /// <summary>
    /// Forcefully terminates the terminal process.
    /// </summary>
    void Terminate();
}
