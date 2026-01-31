using Wslr.Core.Models;

namespace Wslr.Core.Interfaces;

/// <summary>
/// Provides operations for reading and writing the .wslconfig file.
/// </summary>
public interface IWslConfigService
{
    /// <summary>
    /// Gets the path to the .wslconfig file.
    /// </summary>
    string ConfigPath { get; }

    /// <summary>
    /// Gets a value indicating whether the .wslconfig file exists.
    /// </summary>
    bool ConfigExists { get; }

    /// <summary>
    /// Reads and parses the .wslconfig file.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The parsed configuration, or a default configuration if the file doesn't exist.</returns>
    Task<WslConfig> ReadConfigAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the configuration to the .wslconfig file, preserving comments.
    /// </summary>
    /// <param name="config">The configuration to write.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task WriteConfigAsync(WslConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a configuration without writing it.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    /// <returns>The validation result.</returns>
    WslConfigValidationResult Validate(WslConfig config);

    /// <summary>
    /// Creates a backup of the current .wslconfig file.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The path to the backup file, or null if no backup was created.</returns>
    Task<string?> CreateBackupAsync(CancellationToken cancellationToken = default);
}
