using System.Text;
using Wslr.Core.Models;

namespace Wslr.Core.Interfaces;

/// <summary>
/// Provides an abstraction for running external processes.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Runs a process asynchronously with the specified arguments.
    /// Uses UTF-16 LE encoding by default (for WSL native commands).
    /// </summary>
    /// <param name="fileName">The name of the executable to run.</param>
    /// <param name="arguments">The command-line arguments.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation, containing the process result.</returns>
    Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a process asynchronously with the specified arguments and encoding.
    /// </summary>
    /// <param name="fileName">The name of the executable to run.</param>
    /// <param name="arguments">The command-line arguments.</param>
    /// <param name="outputEncoding">The encoding to use for reading output (null for system default).</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation, containing the process result.</returns>
    Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        Encoding? outputEncoding,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a process asynchronously with the specified arguments and reports progress.
    /// </summary>
    /// <param name="fileName">The name of the executable to run.</param>
    /// <param name="arguments">The command-line arguments.</param>
    /// <param name="outputHandler">A callback invoked for each line of standard output.</param>
    /// <param name="errorHandler">A callback invoked for each line of standard error.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation, containing the exit code.</returns>
    Task<int> RunWithOutputAsync(
        string fileName,
        string arguments,
        Action<string>? outputHandler,
        Action<string>? errorHandler,
        CancellationToken cancellationToken = default);
}
