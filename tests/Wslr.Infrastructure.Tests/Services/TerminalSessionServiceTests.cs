using Wslr.Core.Interfaces;
using Wslr.Infrastructure.Services;

namespace Wslr.Infrastructure.Tests.Services;

public class TerminalSessionServiceTests
{
    private readonly TerminalSessionService _sut;

    public TerminalSessionServiceTests()
    {
        _sut = new TerminalSessionService();
    }

    #region CreateSessionAsync Tests

    [Fact]
    public async Task CreateSessionAsync_WithNullDistributionName_ThrowsArgumentException()
    {
        var act = async () => await _sut.CreateSessionAsync(null!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateSessionAsync_WithEmptyDistributionName_ThrowsArgumentException()
    {
        var act = async () => await _sut.CreateSessionAsync("");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateSessionAsync_WithWhitespaceDistributionName_ThrowsArgumentException()
    {
        var act = async () => await _sut.CreateSessionAsync("   ");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateSessionAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        await _sut.DisposeAsync();

        var act = async () => await _sut.CreateSessionAsync("Ubuntu");

        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region ActiveSessions Tests

    [Fact]
    public void ActiveSessions_Initially_IsEmpty()
    {
        _sut.ActiveSessions.Should().BeEmpty();
    }

    #endregion

    #region TerminateAllAsync Tests

    [Fact]
    public async Task TerminateAllAsync_WithNoSessions_DoesNotThrow()
    {
        var act = async () => await _sut.TerminateAllAsync();

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_MultipleCalls_DoesNotThrow()
    {
        await _sut.DisposeAsync();

        var act = async () => await _sut.DisposeAsync();

        await act.Should().NotThrowAsync();
    }

    #endregion
}
