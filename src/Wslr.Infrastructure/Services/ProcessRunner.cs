using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Wslr.Core.Interfaces;
using Wslr.Core.Models;

namespace Wslr.Infrastructure.Services;

/// <summary>
/// Implementation of <see cref="IProcessRunner"/> that executes external processes.
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
    private readonly ILogger<ProcessRunner> _logger;
    private const int MaxOutputLogLength = 1000;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessRunner"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ProcessRunner(ILogger<ProcessRunner> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken = default)
    {
        // Default to UTF-16 LE for WSL native commands (--list, --status, etc.)
        return RunAsync(fileName, arguments, Encoding.Unicode, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        Encoding? outputEncoding,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing: {FileName} {Arguments}", fileName, arguments);
        var stopwatch = Stopwatch.StartNew();

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = outputEncoding,
            StandardErrorEncoding = outputEncoding
        };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);
        stopwatch.Stop();

        var result = new ProcessResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = outputBuilder.ToString(),
            StandardError = errorBuilder.ToString()
        };

        _logger.LogDebug(
            "Completed: {FileName} {Arguments} (ExitCode={ExitCode}, Duration={Duration}ms)",
            fileName,
            arguments,
            result.ExitCode,
            stopwatch.ElapsedMilliseconds);

        if (_logger.IsEnabled(LogLevel.Debug) && !string.IsNullOrEmpty(result.StandardOutput))
        {
            var truncatedOutput = result.StandardOutput.Length > MaxOutputLogLength
                ? result.StandardOutput[..MaxOutputLogLength] + "..."
                : result.StandardOutput;
            _logger.LogDebug("Output: {Output}", truncatedOutput.Trim());
        }

        if (!string.IsNullOrEmpty(result.StandardError))
        {
            var truncatedError = result.StandardError.Length > MaxOutputLogLength
                ? result.StandardError[..MaxOutputLogLength] + "..."
                : result.StandardError;
            _logger.LogWarning("StdErr: {Error}", truncatedError.Trim());
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<int> RunWithOutputAsync(
        string fileName,
        string arguments,
        Action<string>? outputHandler,
        Action<string>? errorHandler,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing (streaming): {FileName} {Arguments}", fileName, arguments);
        var stopwatch = Stopwatch.StartNew();

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            // WSL outputs UTF-16 LE (Unicode), not UTF-8
            StandardOutputEncoding = Encoding.Unicode,
            StandardErrorEncoding = Encoding.Unicode
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                outputHandler?.Invoke(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                errorHandler?.Invoke(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);
        stopwatch.Stop();

        var exitCode = process.ExitCode;

        _logger.LogDebug(
            "Completed (streaming): {FileName} {Arguments} (ExitCode={ExitCode}, Duration={Duration}ms)",
            fileName,
            arguments,
            exitCode,
            stopwatch.ElapsedMilliseconds);

        return exitCode;
    }
}
