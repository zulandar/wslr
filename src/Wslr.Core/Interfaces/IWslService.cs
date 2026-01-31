using Wslr.Core.Models;

namespace Wslr.Core.Interfaces;

/// <summary>
/// Provides operations for managing WSL distributions.
/// </summary>
public interface IWslService
{
    /// <summary>
    /// Gets all installed WSL distributions with their current state.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation, containing the list of distributions.</returns>
    Task<IReadOnlyList<WslDistribution>> GetDistributionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available online distributions that can be installed.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation, containing the list of available distributions.</returns>
    Task<IReadOnlyList<OnlineDistribution>> GetOnlineDistributionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Installs a distribution from the online catalog.
    /// </summary>
    /// <param name="distributionName">The name of the distribution to install.</param>
    /// <param name="progress">Optional progress reporter for installation status.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task InstallDistributionAsync(
        string distributionName,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters (deletes) a distribution.
    /// </summary>
    /// <param name="distributionName">The name of the distribution to unregister.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task UnregisterDistributionAsync(string distributionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a distribution to a tar file.
    /// </summary>
    /// <param name="distributionName">The name of the distribution to export.</param>
    /// <param name="exportPath">The path where the tar file will be created.</param>
    /// <param name="progress">Optional progress reporter for export status.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ExportDistributionAsync(
        string distributionName,
        string exportPath,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a distribution from a tar file.
    /// </summary>
    /// <param name="distributionName">The name for the new distribution.</param>
    /// <param name="installLocation">The directory where the distribution will be installed.</param>
    /// <param name="tarPath">The path to the tar file to import.</param>
    /// <param name="version">The WSL version to use (1 or 2). Defaults to 2.</param>
    /// <param name="progress">Optional progress reporter for import status.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ImportDistributionAsync(
        string distributionName,
        string installLocation,
        string tarPath,
        int version = 2,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a distribution.
    /// </summary>
    /// <param name="distributionName">The name of the distribution to start.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task StartDistributionAsync(string distributionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Terminates (stops) a running distribution.
    /// </summary>
    /// <param name="distributionName">The name of the distribution to terminate.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task TerminateDistributionAsync(string distributionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shuts down all running WSL distributions.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ShutdownAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the default WSL distribution.
    /// </summary>
    /// <param name="distributionName">The name of the distribution to set as default.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetDefaultDistributionAsync(string distributionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a command inside a distribution.
    /// </summary>
    /// <param name="distributionName">The name of the distribution.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation, containing the process result.</returns>
    Task<ProcessResult> ExecuteCommandAsync(
        string distributionName,
        string command,
        CancellationToken cancellationToken = default);
}
