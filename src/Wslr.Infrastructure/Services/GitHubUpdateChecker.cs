using System.Net.Http.Json;
using System.Reflection;
using Wslr.Core.Interfaces;
using Wslr.Core.Models;

namespace Wslr.Infrastructure.Services;

/// <summary>
/// Checks for updates by querying the GitHub Releases API.
/// </summary>
public class GitHubUpdateChecker : IUpdateChecker
{
    private readonly HttpClient _httpClient;
    private readonly string _repositoryOwner;
    private readonly string _repositoryName;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubUpdateChecker"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for API requests.</param>
    /// <param name="repositoryOwner">The GitHub repository owner.</param>
    /// <param name="repositoryName">The GitHub repository name.</param>
    public GitHubUpdateChecker(HttpClient httpClient, string repositoryOwner, string repositoryName)
    {
        _httpClient = httpClient;
        _repositoryOwner = repositoryOwner;
        _repositoryName = repositoryName;
    }

    /// <inheritdoc />
    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var currentVersion = GetCurrentVersion();

        try
        {
            var release = await GetLatestReleaseAsync(cancellationToken);

            if (release is null)
            {
                return UpdateCheckResult.NoUpdate(currentVersion);
            }

            var latestVersion = ParseVersion(release.TagName);

            if (latestVersion is null)
            {
                return UpdateCheckResult.NoUpdate(currentVersion);
            }

            var updateAvailable = currentVersion is not null && latestVersion > currentVersion;

            // Find the ZIP asset
            var zipAsset = release.Assets.FirstOrDefault(a =>
                a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

            return new UpdateCheckResult
            {
                UpdateAvailable = updateAvailable,
                CurrentVersion = currentVersion,
                LatestVersion = latestVersion,
                ReleaseUrl = release.HtmlUrl,
                DownloadUrl = zipAsset?.BrowserDownloadUrl,
                ReleaseNotes = release.Body
            };
        }
        catch (HttpRequestException)
        {
            // Network error - fail silently
            return UpdateCheckResult.NoUpdate(currentVersion);
        }
        catch (TaskCanceledException)
        {
            // Timeout or cancellation - fail silently
            return UpdateCheckResult.NoUpdate(currentVersion);
        }
    }

    private async Task<GitHubRelease?> GetLatestReleaseAsync(CancellationToken cancellationToken)
    {
        var url = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}/releases/latest";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept", "application/vnd.github.v3+json");

        // GitHub API requires a User-Agent header
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            request.Headers.Add("User-Agent", "Wslr");
        }

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<GitHubRelease>(cancellationToken);
    }

    private static Version? GetCurrentVersion()
    {
        var infoVersion = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (string.IsNullOrEmpty(infoVersion))
        {
            return null;
        }

        // MinVer format: "0.1.0" or "0.1.0+build.123" - strip build metadata
        var versionString = infoVersion.Split('+')[0];
        return Version.TryParse(versionString, out var version) ? version : null;
    }

    private static Version? ParseVersion(string tagName)
    {
        // Strip 'v' prefix if present
        var versionString = tagName.TrimStart('v', 'V');

        // Try to parse as Version (handles major.minor.build.revision)
        if (Version.TryParse(versionString, out var version))
        {
            return version;
        }

        // Handle semver with prerelease (e.g., "1.0.0-alpha")
        var dashIndex = versionString.IndexOf('-');
        if (dashIndex > 0)
        {
            versionString = versionString[..dashIndex];
            if (Version.TryParse(versionString, out version))
            {
                return version;
            }
        }

        return null;
    }
}
