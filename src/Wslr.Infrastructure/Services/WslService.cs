using System.Text;
using Wslr.Core.Exceptions;
using Wslr.Core.Interfaces;
using Wslr.Core.Models;
using Wslr.Infrastructure.Parsing;

namespace Wslr.Infrastructure.Services;

/// <summary>
/// Implementation of <see cref="IWslService"/> that wraps the WSL CLI.
/// </summary>
public sealed class WslService : IWslService
{
    private const string WslExecutable = "wsl.exe";
    private readonly IProcessRunner _processRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="WslService"/> class.
    /// </summary>
    /// <param name="processRunner">The process runner to use for executing commands.</param>
    public WslService(IProcessRunner processRunner)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WslDistribution>> GetDistributionsAsync(CancellationToken cancellationToken = default)
    {
        var result = await _processRunner.RunAsync(WslExecutable, "--list --verbose", cancellationToken);

        if (!result.IsSuccess)
        {
            // No distributions installed returns exit code 0 but with specific message
            // Some error conditions return non-zero
            if (result.StandardError.Contains("no installed distributions", StringComparison.OrdinalIgnoreCase))
            {
                return [];
            }

            throw new WslException(
                "Failed to get WSL distributions",
                result.ExitCode,
                result.StandardError);
        }

        return WslOutputParser.ParseListVerbose(result.StandardOutput);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OnlineDistribution>> GetOnlineDistributionsAsync(CancellationToken cancellationToken = default)
    {
        var result = await _processRunner.RunAsync(WslExecutable, "--list --online", cancellationToken);

        if (!result.IsSuccess)
        {
            throw new WslException(
                "Failed to get online distributions",
                result.ExitCode,
                result.StandardError);
        }

        return WslOutputParser.ParseListOnline(result.StandardOutput);
    }

    /// <inheritdoc />
    public async Task InstallDistributionAsync(
        string distributionName,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distributionName);

        progress?.Report($"Installing {distributionName}...");

        var exitCode = await _processRunner.RunWithOutputAsync(
            WslExecutable,
            $"--install -d {distributionName}",
            line => progress?.Report(line),
            line => progress?.Report($"[Error] {line}"),
            cancellationToken);

        if (exitCode != 0)
        {
            throw new WslException($"Failed to install distribution '{distributionName}'", exitCode, null);
        }

        progress?.Report($"Successfully installed {distributionName}");
    }

    /// <inheritdoc />
    public async Task UnregisterDistributionAsync(string distributionName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distributionName);

        var result = await _processRunner.RunAsync(
            WslExecutable,
            $"--unregister {distributionName}",
            cancellationToken);

        if (!result.IsSuccess)
        {
            throw new WslException(
                $"Failed to unregister distribution '{distributionName}'",
                result.ExitCode,
                result.StandardError);
        }
    }

    /// <inheritdoc />
    public async Task ExportDistributionAsync(
        string distributionName,
        string exportPath,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distributionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(exportPath);

        progress?.Report($"Exporting {distributionName} to {exportPath}...");

        var exitCode = await _processRunner.RunWithOutputAsync(
            WslExecutable,
            $"--export {distributionName} \"{exportPath}\"",
            line => progress?.Report(line),
            line => progress?.Report($"[Error] {line}"),
            cancellationToken);

        if (exitCode != 0)
        {
            throw new WslException($"Failed to export distribution '{distributionName}'", exitCode, null);
        }

        progress?.Report($"Successfully exported {distributionName}");
    }

    /// <inheritdoc />
    public async Task ImportDistributionAsync(
        string distributionName,
        string installLocation,
        string tarPath,
        int version = 2,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distributionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(installLocation);
        ArgumentException.ThrowIfNullOrWhiteSpace(tarPath);

        if (version is not 1 and not 2)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "WSL version must be 1 or 2");
        }

        progress?.Report($"Importing {distributionName} from {tarPath}...");

        var exitCode = await _processRunner.RunWithOutputAsync(
            WslExecutable,
            $"--import {distributionName} \"{installLocation}\" \"{tarPath}\" --version {version}",
            line => progress?.Report(line),
            line => progress?.Report($"[Error] {line}"),
            cancellationToken);

        if (exitCode != 0)
        {
            throw new WslException($"Failed to import distribution '{distributionName}'", exitCode, null);
        }

        progress?.Report($"Successfully imported {distributionName}");
    }

    /// <inheritdoc />
    public async Task StartDistributionAsync(string distributionName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distributionName);

        // Starting a distribution by running a simple command
        var result = await _processRunner.RunAsync(
            WslExecutable,
            $"-d {distributionName} --exec exit",
            cancellationToken);

        if (!result.IsSuccess)
        {
            throw new WslException(
                $"Failed to start distribution '{distributionName}'",
                result.ExitCode,
                result.StandardError);
        }
    }

    /// <inheritdoc />
    public async Task TerminateDistributionAsync(string distributionName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distributionName);

        var result = await _processRunner.RunAsync(
            WslExecutable,
            $"--terminate {distributionName}",
            cancellationToken);

        if (!result.IsSuccess)
        {
            throw new WslException(
                $"Failed to terminate distribution '{distributionName}'",
                result.ExitCode,
                result.StandardError);
        }
    }

    /// <inheritdoc />
    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        var result = await _processRunner.RunAsync(WslExecutable, "--shutdown", cancellationToken);

        if (!result.IsSuccess)
        {
            throw new WslException(
                "Failed to shutdown WSL",
                result.ExitCode,
                result.StandardError);
        }
    }

    /// <inheritdoc />
    public async Task SetDefaultDistributionAsync(string distributionName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distributionName);

        var result = await _processRunner.RunAsync(
            WslExecutable,
            $"--set-default {distributionName}",
            cancellationToken);

        if (!result.IsSuccess)
        {
            throw new WslException(
                $"Failed to set default distribution '{distributionName}'",
                result.ExitCode,
                result.StandardError);
        }
    }

    /// <inheritdoc />
    public async Task<ProcessResult> ExecuteCommandAsync(
        string distributionName,
        string command,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distributionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(command);

        // Commands executed inside WSL distributions output UTF-8 (from Linux),
        // unlike WSL native commands (--list, etc.) which output UTF-16 LE
        return await _processRunner.RunAsync(
            WslExecutable,
            $"-d {distributionName} -- {command}",
            Encoding.UTF8,
            cancellationToken);
    }
}
