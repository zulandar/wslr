using Wslr.Core.Models;

namespace Wslr.Core.Interfaces;

/// <summary>
/// Provides operations for reading and writing wsl.conf files for WSL distributions.
/// </summary>
public interface IWslDistroConfigService
{
    /// <summary>
    /// Gets the path to the wsl.conf file for a distribution.
    /// </summary>
    /// <param name="distributionName">The distribution name.</param>
    /// <returns>The path to the wsl.conf file.</returns>
    string GetConfigPath(string distributionName);

    /// <summary>
    /// Gets a value indicating whether the wsl.conf file exists for a distribution.
    /// </summary>
    /// <param name="distributionName">The distribution name.</param>
    /// <returns>True if the config file exists.</returns>
    bool ConfigExists(string distributionName);

    /// <summary>
    /// Reads and parses the wsl.conf file for a distribution.
    /// </summary>
    /// <param name="distributionName">The distribution name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The parsed configuration, or a default configuration if the file doesn't exist.</returns>
    Task<WslDistroConfig> ReadConfigAsync(string distributionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the configuration to the wsl.conf file for a distribution.
    /// </summary>
    /// <param name="distributionName">The distribution name.</param>
    /// <param name="config">The configuration to write.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task WriteConfigAsync(string distributionName, WslDistroConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a configuration without writing it.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    /// <returns>The validation result.</returns>
    WslDistroConfigValidationResult Validate(WslDistroConfig config);

    /// <summary>
    /// Creates a backup of the current wsl.conf file for a distribution.
    /// </summary>
    /// <param name="distributionName">The distribution name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The path to the backup file, or null if no backup was created.</returns>
    Task<string?> CreateBackupAsync(string distributionName, CancellationToken cancellationToken = default);
}
