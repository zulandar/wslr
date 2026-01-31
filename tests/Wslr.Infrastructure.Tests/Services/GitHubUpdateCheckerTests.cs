using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using Wslr.Core.Models;
using Wslr.Infrastructure.Services;

namespace Wslr.Infrastructure.Tests.Services;

public class GitHubUpdateCheckerTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly GitHubUpdateChecker _sut;

    public GitHubUpdateCheckerTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _sut = new GitHubUpdateChecker(_httpClient, "testowner", "testrepo");
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenNewerVersionAvailable_ReturnsUpdateAvailable()
    {
        var release = new GitHubRelease
        {
            TagName = "v99.0.0",
            HtmlUrl = "https://github.com/testowner/testrepo/releases/tag/v99.0.0",
            Body = "Release notes",
            Assets =
            [
                new GitHubReleaseAsset
                {
                    Name = "Wslr-v99.0.0.zip",
                    BrowserDownloadUrl = "https://github.com/testowner/testrepo/releases/download/v99.0.0/Wslr-v99.0.0.zip"
                }
            ]
        };

        SetupHttpResponse(HttpStatusCode.OK, release);

        var result = await _sut.CheckForUpdatesAsync();

        result.UpdateAvailable.Should().BeTrue();
        result.LatestVersion.Should().Be(new Version(99, 0, 0));
        result.ReleaseUrl.Should().Be("https://github.com/testowner/testrepo/releases/tag/v99.0.0");
        result.DownloadUrl.Should().Contain("Wslr-v99.0.0.zip");
        result.ReleaseNotes.Should().Be("Release notes");
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenSameVersion_ReturnsNoUpdate()
    {
        // Use version 0.0.0 which matches untagged builds
        var release = new GitHubRelease
        {
            TagName = "v0.0.0",
            HtmlUrl = "https://github.com/testowner/testrepo/releases/tag/v0.0.0",
            Assets = []
        };

        SetupHttpResponse(HttpStatusCode.OK, release);

        var result = await _sut.CheckForUpdatesAsync();

        result.UpdateAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenHttpError_ReturnsNoUpdate()
    {
        SetupHttpResponse(HttpStatusCode.NotFound, null);

        var result = await _sut.CheckForUpdatesAsync();

        result.UpdateAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenNetworkError_ReturnsNoUpdate()
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var result = await _sut.CheckForUpdatesAsync();

        result.UpdateAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenInvalidTagName_ReturnsNoUpdate()
    {
        var release = new GitHubRelease
        {
            TagName = "invalid-tag",
            HtmlUrl = "https://github.com/testowner/testrepo/releases/tag/invalid-tag",
            Assets = []
        };

        SetupHttpResponse(HttpStatusCode.OK, release);

        var result = await _sut.CheckForUpdatesAsync();

        result.UpdateAvailable.Should().BeFalse();
    }

    [Theory]
    [InlineData("v1.2.3", 1, 2, 3)]
    [InlineData("V1.2.3", 1, 2, 3)]
    [InlineData("1.2.3", 1, 2, 3)]
    [InlineData("v1.0.0-alpha", 1, 0, 0)]
    [InlineData("v2.1.0-beta.1", 2, 1, 0)]
    public async Task CheckForUpdatesAsync_ParsesVersionCorrectly(string tagName, int major, int minor, int build)
    {
        var release = new GitHubRelease
        {
            TagName = tagName,
            HtmlUrl = "https://example.com",
            Assets = []
        };

        SetupHttpResponse(HttpStatusCode.OK, release);

        var result = await _sut.CheckForUpdatesAsync();

        result.LatestVersion.Should().NotBeNull();
        result.LatestVersion!.Major.Should().Be(major);
        result.LatestVersion.Minor.Should().Be(minor);
        result.LatestVersion.Build.Should().Be(build);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, GitHubRelease? release)
    {
        var response = new HttpResponseMessage(statusCode);

        if (release is not null)
        {
            response.Content = new StringContent(JsonSerializer.Serialize(release));
        }

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }
}
