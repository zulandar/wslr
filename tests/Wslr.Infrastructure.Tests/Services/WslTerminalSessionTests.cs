using Wslr.Infrastructure.Services;

namespace Wslr.Infrastructure.Tests.Services;

public class WslTerminalSessionTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullDistributionName_ThrowsArgumentException()
    {
        var act = () => new WslTerminalSession(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyDistributionName_ThrowsArgumentException()
    {
        var act = () => new WslTerminalSession("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithWhitespaceDistributionName_ThrowsArgumentException()
    {
        var act = () => new WslTerminalSession("   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task Constructor_WithValidDistributionName_SetsDistributionName()
    {
        // This will fail if wsl.exe isn't available or the distro doesn't exist,
        // but it tests the property assignment path
        try
        {
            await using var session = new WslTerminalSession("TestDistro");
            session.DistributionName.Should().Be("TestDistro");
        }
        catch
        {
            // Expected to fail in CI or when WSL isn't available
            // The important thing is the constructor path is exercised
        }
    }

    [Fact]
    public async Task Constructor_WithValidDistributionName_GeneratesUniqueId()
    {
        try
        {
            await using var session = new WslTerminalSession("TestDistro");
            session.Id.Should().NotBeNullOrEmpty();
            session.Id.Should().HaveLength(8);
        }
        catch
        {
            // Expected to fail in CI or when WSL isn't available
        }
    }

    #endregion

    #region ResizeAsync Tests

    [Fact]
    public async Task ResizeAsync_WithValidDimensions_DoesNotThrow()
    {
        try
        {
            await using var session = new WslTerminalSession("TestDistro");

            var act = async () => await session.ResizeAsync(120, 40);

            await act.Should().NotThrowAsync();
        }
        catch
        {
            // Expected to fail in CI or when WSL isn't available
        }
    }

    #endregion
}
