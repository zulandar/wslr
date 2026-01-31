using System.Text.Json.Serialization;

namespace Wslr.Core.Models;

/// <summary>
/// Represents a GitHub release from the GitHub API.
/// </summary>
public record GitHubRelease
{
    /// <summary>
    /// Gets the tag name (e.g., "v1.2.0").
    /// </summary>
    [JsonPropertyName("tag_name")]
    public string TagName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the URL to the release page.
    /// </summary>
    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; init; } = string.Empty;

    /// <summary>
    /// Gets the release body/description (markdown).
    /// </summary>
    [JsonPropertyName("body")]
    public string? Body { get; init; }

    /// <summary>
    /// Gets the release name/title.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// Gets whether this is a prerelease.
    /// </summary>
    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; init; }

    /// <summary>
    /// Gets whether this is a draft release.
    /// </summary>
    [JsonPropertyName("draft")]
    public bool Draft { get; init; }

    /// <summary>
    /// Gets the release assets.
    /// </summary>
    [JsonPropertyName("assets")]
    public List<GitHubReleaseAsset> Assets { get; init; } = [];
}

/// <summary>
/// Represents an asset attached to a GitHub release.
/// </summary>
public record GitHubReleaseAsset
{
    /// <summary>
    /// Gets the asset filename.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the direct download URL for the asset.
    /// </summary>
    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; init; } = string.Empty;

    /// <summary>
    /// Gets the asset size in bytes.
    /// </summary>
    [JsonPropertyName("size")]
    public long Size { get; init; }

    /// <summary>
    /// Gets the content type of the asset.
    /// </summary>
    [JsonPropertyName("content_type")]
    public string? ContentType { get; init; }
}
