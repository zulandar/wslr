namespace Wslr.Core.Models;

/// <summary>
/// Result of executing a setup script in a WSL distribution.
/// </summary>
public sealed record ScriptExecutionResult
{
    /// <summary>
    /// Exit code from the script execution.
    /// </summary>
    public required int ExitCode { get; init; }

    /// <summary>
    /// Combined stdout output from the script.
    /// </summary>
    public required string StandardOutput { get; init; }

    /// <summary>
    /// Combined stderr output from the script.
    /// </summary>
    public required string StandardError { get; init; }

    /// <summary>
    /// Duration of script execution.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Whether the script completed successfully (exit code 0).
    /// </summary>
    public bool IsSuccess => ExitCode == 0;

    /// <summary>
    /// Whether the script was cancelled before completion.
    /// </summary>
    public bool WasCancelled { get; init; }
}
