using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Wslr.Core.Interfaces;
using Wslr.Core.Models;

namespace Wslr.Infrastructure.Services;

/// <summary>
/// Service for executing bash scripts inside WSL distributions.
/// </summary>
public sealed partial class ScriptExecutionService : IScriptExecutionService
{
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<ScriptExecutionService> _logger;

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(30);

    public ScriptExecutionService(
        IProcessRunner processRunner,
        ILogger<ScriptExecutionService> logger)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ScriptExecutionResult> ExecuteScriptAsync(
        string distributionName,
        string scriptContent,
        IReadOnlyDictionary<string, string>? variables = null,
        IProgress<string>? progress = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distributionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptContent);

        _logger.LogInformation("Executing script in distribution '{Distribution}'", distributionName);

        // Substitute variables if provided
        var processedScript = SubstituteVariables(scriptContent, variables);

        var stopwatch = Stopwatch.StartNew();
        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();

        try
        {
            // Create a combined cancellation token with timeout
            using var timeoutCts = new CancellationTokenSource(timeout ?? DefaultTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            // Execute script via bash -c with the script passed via stdin to handle complex scripts
            // We use a here-document approach to pass the script safely
            var escapedScript = processedScript.Replace("'", "'\\''");
            var command = $"-d {distributionName} -- bash -c '{escapedScript}'";

            var exitCode = await _processRunner.RunWithOutputAsync(
                "wsl.exe",
                command,
                line =>
                {
                    stdoutBuilder.AppendLine(line);
                    progress?.Report(line);
                },
                line =>
                {
                    stderrBuilder.AppendLine(line);
                    progress?.Report($"[stderr] {line}");
                },
                linkedCts.Token);

            stopwatch.Stop();

            _logger.LogInformation(
                "Script completed in {Duration}ms with exit code {ExitCode}",
                stopwatch.ElapsedMilliseconds,
                exitCode);

            return new ScriptExecutionResult
            {
                ExitCode = exitCode,
                StandardOutput = stdoutBuilder.ToString(),
                StandardError = stderrBuilder.ToString(),
                Duration = stopwatch.Elapsed,
                WasCancelled = false
            };
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();

            _logger.LogWarning(
                "Script execution cancelled after {Duration}ms",
                stopwatch.ElapsedMilliseconds);

            return new ScriptExecutionResult
            {
                ExitCode = -1,
                StandardOutput = stdoutBuilder.ToString(),
                StandardError = stderrBuilder.ToString(),
                Duration = stopwatch.Elapsed,
                WasCancelled = true
            };
        }
    }

    /// <inheritdoc />
    public async Task<ScriptExecutionResult> ExecuteTemplateAsync(
        string distributionName,
        ScriptTemplate template,
        IReadOnlyDictionary<string, string>? variableOverrides = null,
        IProgress<string>? progress = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);

        _logger.LogInformation(
            "Executing template '{TemplateName}' in distribution '{Distribution}'",
            template.Name,
            distributionName);

        // Merge template variables with overrides
        var mergedVariables = new Dictionary<string, string>();

        if (template.Variables != null)
        {
            foreach (var (key, value) in template.Variables)
            {
                mergedVariables[key] = value;
            }
        }

        if (variableOverrides != null)
        {
            foreach (var (key, value) in variableOverrides)
            {
                mergedVariables[key] = value;
            }
        }

        return await ExecuteScriptAsync(
            distributionName,
            template.ScriptContent,
            mergedVariables,
            progress,
            timeout,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ScriptValidationResult> ValidateScriptAsync(
        string distributionName,
        string scriptContent,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distributionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptContent);

        _logger.LogDebug("Validating script syntax in distribution '{Distribution}'", distributionName);

        try
        {
            // Use bash -n to check syntax without executing
            var escapedScript = scriptContent.Replace("'", "'\\''");
            var command = $"-d {distributionName} -- bash -n -c '{escapedScript}'";

            var result = await _processRunner.RunAsync(
                "wsl.exe",
                command,
                Encoding.UTF8,
                cancellationToken);

            if (result.IsSuccess)
            {
                return ScriptValidationResult.Success;
            }

            // Try to parse the error message for line number
            var errorMessage = result.StandardError.Trim();
            int? errorLine = null;

            var lineMatch = LineNumberRegex().Match(errorMessage);
            if (lineMatch.Success && int.TryParse(lineMatch.Groups[1].Value, out var line))
            {
                errorLine = line;
            }

            return ScriptValidationResult.Failure(errorMessage, errorLine);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate script");
            return ScriptValidationResult.Failure($"Validation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Substitutes variables in the script content.
    /// Variables are in the format ${VARIABLE_NAME}.
    /// </summary>
    private static string SubstituteVariables(string scriptContent, IReadOnlyDictionary<string, string>? variables)
    {
        if (variables == null || variables.Count == 0)
        {
            return scriptContent;
        }

        var result = scriptContent;
        foreach (var (name, value) in variables)
        {
            // Replace ${VARIABLE_NAME} with the value
            result = result.Replace($"${{{name}}}", value);
        }

        return result;
    }

    [GeneratedRegex(@"line (\d+):")]
    private static partial Regex LineNumberRegex();
}
