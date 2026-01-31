namespace Wslr.Core.Interfaces;

/// <summary>
/// Provides functionality to check for application updates.
/// </summary>
public interface IUpdateChecker
{
    /// <summary>
    /// Checks for available updates asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task containing the update check result.</returns>
    Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of an update check.
/// </summary>
public record UpdateCheckResult
{
    /// <summary>
    /// Gets whether an update is available.
    /// </summary>
    public bool UpdateAvailable { get; init; }

    /// <summary>
    /// Gets the current application version.
    /// </summary>
    public Version? CurrentVersion { get; init; }

    /// <summary>
    /// Gets the latest available version.
    /// </summary>
    public Version? LatestVersion { get; init; }

    /// <summary>
    /// Gets the URL to the release page.
    /// </summary>
    public string? ReleaseUrl { get; init; }

    /// <summary>
    /// Gets the direct download URL for the release asset.
    /// </summary>
    public string? DownloadUrl { get; init; }

    /// <summary>
    /// Gets the release notes/description.
    /// </summary>
    public string? ReleaseNotes { get; init; }

    /// <summary>
    /// Creates a result indicating no update is available.
    /// </summary>
    public static UpdateCheckResult NoUpdate(Version? currentVersion) => new()
    {
        UpdateAvailable = false,
        CurrentVersion = currentVersion
    };
}
