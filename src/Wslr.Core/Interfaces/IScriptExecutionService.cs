using Wslr.Core.Models;

namespace Wslr.Core.Interfaces;

/// <summary>
/// Service for executing bash scripts inside WSL distributions.
/// </summary>
public interface IScriptExecutionService
{
    /// <summary>
    /// Executes a bash script in the specified distribution.
    /// </summary>
    /// <param name="distributionName">The name of the WSL distribution.</param>
    /// <param name="scriptContent">The bash script content to execute.</param>
    /// <param name="variables">Optional variables to substitute in the script.</param>
    /// <param name="progress">Optional progress reporter for real-time output.</param>
    /// <param name="timeout">Optional timeout for script execution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the script execution.</returns>
    Task<ScriptExecutionResult> ExecuteScriptAsync(
        string distributionName,
        string scriptContent,
        IReadOnlyDictionary<string, string>? variables = null,
        IProgress<string>? progress = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a script template in the specified distribution.
    /// </summary>
    /// <param name="distributionName">The name of the WSL distribution.</param>
    /// <param name="template">The script template to execute.</param>
    /// <param name="variableOverrides">Optional variable overrides (merged with template defaults).</param>
    /// <param name="progress">Optional progress reporter for real-time output.</param>
    /// <param name="timeout">Optional timeout for script execution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the script execution.</returns>
    Task<ScriptExecutionResult> ExecuteTemplateAsync(
        string distributionName,
        ScriptTemplate template,
        IReadOnlyDictionary<string, string>? variableOverrides = null,
        IProgress<string>? progress = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a script for basic syntax errors without executing it.
    /// </summary>
    /// <param name="distributionName">The name of the WSL distribution to use for validation.</param>
    /// <param name="scriptContent">The bash script content to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with any errors found.</returns>
    Task<ScriptValidationResult> ValidateScriptAsync(
        string distributionName,
        string scriptContent,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of script validation.
/// </summary>
public sealed record ScriptValidationResult
{
    /// <summary>
    /// Whether the script is valid.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Line number where the error occurred, if applicable.
    /// </summary>
    public int? ErrorLine { get; init; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ScriptValidationResult Success => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static ScriptValidationResult Failure(string errorMessage, int? errorLine = null) =>
        new() { IsValid = false, ErrorMessage = errorMessage, ErrorLine = errorLine };
}
