namespace Wslr.Core.Models;

/// <summary>
/// Represents the result of a process execution.
/// </summary>
public sealed record ProcessResult
{
    /// <summary>
    /// Gets the exit code of the process.
    /// </summary>
    public required int ExitCode { get; init; }

    /// <summary>
    /// Gets the standard output of the process.
    /// </summary>
    public required string StandardOutput { get; init; }

    /// <summary>
    /// Gets the standard error output of the process.
    /// </summary>
    public required string StandardError { get; init; }

    /// <summary>
    /// Gets a value indicating whether the process completed successfully (exit code 0).
    /// </summary>
    public bool IsSuccess => ExitCode == 0;
}
