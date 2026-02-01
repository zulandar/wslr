using Wslr.Core.Interfaces;
using Wslr.UI.ViewModels;

namespace Wslr.UI.Tests.ViewModels;

public class TerminalViewModelTests
{
    private readonly Mock<ITerminalSessionService> _sessionServiceMock;
    private readonly TerminalViewModel _sut;

    public TerminalViewModelTests()
    {
        _sessionServiceMock = new Mock<ITerminalSessionService>();
        _sut = new TerminalViewModel(_sessionServiceMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullSessionService_ThrowsArgumentNullException()
    {
        var act = () => new TerminalViewModel(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("sessionService");
    }

    [Fact]
    public void Constructor_InitializesWithEmptyState()
    {
        _sut.HasTabs.Should().BeFalse();
        _sut.ActiveTab.Should().BeNull();
        _sut.Tabs.Should().BeEmpty();
        _sut.ErrorMessage.Should().BeNull();
    }

    #endregion

    #region OpenTabAsync Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task OpenTabAsync_WithInvalidDistributionName_SetsErrorMessage(string? distributionName)
    {
        await _sut.OpenTabAsync(distributionName!);

        _sut.ErrorMessage.Should().NotBeNullOrEmpty();
        _sut.Tabs.Should().BeEmpty();
    }

    [Fact]
    public async Task OpenTabAsync_WithValidDistributionName_CreatesTabAndSession()
    {
        var mockSession = new Mock<ITerminalSession>();
        mockSession.Setup(s => s.Id).Returns("test123");
        mockSession.Setup(s => s.DistributionName).Returns("Ubuntu");

        _sessionServiceMock
            .Setup(s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession.Object);

        await _sut.OpenTabAsync("Ubuntu");

        _sut.Tabs.Should().HaveCount(1);
        _sut.ActiveTab.Should().NotBeNull();
        _sut.ActiveTab!.DistributionName.Should().Be("Ubuntu");
        _sut.ActiveTab.IsConnected.Should().BeTrue();
        _sut.HasTabs.Should().BeTrue();
    }

    [Fact]
    public async Task OpenTabAsync_WhenSessionCreationFails_StillCreatesTabWithError()
    {
        _sessionServiceMock
            .Setup(s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection failed"));

        await _sut.OpenTabAsync("Ubuntu");

        _sut.Tabs.Should().HaveCount(1);
        _sut.ActiveTab.Should().NotBeNull();
        _sut.ActiveTab!.IsConnected.Should().BeFalse();
        _sut.ActiveTab.ErrorMessage.Should().Contain("Connection failed");
    }

    [Fact]
    public async Task OpenTabAsync_MultipleTabs_AllAreTracked()
    {
        var mockSession1 = new Mock<ITerminalSession>();
        var mockSession2 = new Mock<ITerminalSession>();

        _sessionServiceMock
            .Setup(s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession1.Object);
        _sessionServiceMock
            .Setup(s => s.CreateSessionAsync("Debian", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession2.Object);

        await _sut.OpenTabAsync("Ubuntu");
        await _sut.OpenTabAsync("Debian");

        _sut.Tabs.Should().HaveCount(2);
        _sut.ActiveTab!.DistributionName.Should().Be("Debian"); // Latest is active
    }

    [Fact]
    public async Task OpenTabAsync_RaisesTabAddedEvent()
    {
        var mockSession = new Mock<ITerminalSession>();
        _sessionServiceMock
            .Setup(s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession.Object);

        TerminalTabViewModel? addedTab = null;
        _sut.TabAdded += tab => addedTab = tab;

        await _sut.OpenTabAsync("Ubuntu");

        addedTab.Should().NotBeNull();
        addedTab!.DistributionName.Should().Be("Ubuntu");
    }

    #endregion

    #region CloseTabAsync Tests

    [Fact]
    public async Task CloseTabAsync_WithNullTab_DoesNothing()
    {
        await _sut.CloseTabAsync(null);

        // No exception should be thrown
    }

    [Fact]
    public async Task CloseTabAsync_WithValidTab_RemovesTabAndDisposesSession()
    {
        var mockSession = new Mock<ITerminalSession>();
        _sessionServiceMock
            .Setup(s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession.Object);

        await _sut.OpenTabAsync("Ubuntu");
        var tab = _sut.ActiveTab;

        await _sut.CloseTabAsync(tab);

        _sut.Tabs.Should().BeEmpty();
        _sut.ActiveTab.Should().BeNull();
        _sut.HasTabs.Should().BeFalse();
        mockSession.Verify(s => s.Terminate(), Times.Once);
    }

    [Fact]
    public async Task CloseTabAsync_RaisesTabRemovedEvent()
    {
        var mockSession = new Mock<ITerminalSession>();
        _sessionServiceMock
            .Setup(s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession.Object);

        await _sut.OpenTabAsync("Ubuntu");
        var tab = _sut.ActiveTab;

        TerminalTabViewModel? removedTab = null;
        _sut.TabRemoved += t => removedTab = t;

        await _sut.CloseTabAsync(tab);

        removedTab.Should().Be(tab);
    }

    [Fact]
    public async Task CloseTabAsync_WhenClosingActiveTab_ActivatesAdjacentTab()
    {
        var mockSession1 = new Mock<ITerminalSession>();
        var mockSession2 = new Mock<ITerminalSession>();

        _sessionServiceMock
            .Setup(s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession1.Object);
        _sessionServiceMock
            .Setup(s => s.CreateSessionAsync("Debian", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession2.Object);

        await _sut.OpenTabAsync("Ubuntu");
        await _sut.OpenTabAsync("Debian");

        var debian = _sut.ActiveTab;
        await _sut.CloseTabAsync(debian);

        _sut.Tabs.Should().HaveCount(1);
        _sut.ActiveTab!.DistributionName.Should().Be("Ubuntu");
    }

    #endregion

    #region ActivateTab Tests

    [Fact]
    public async Task ActivateTab_SwitchesToSpecifiedTab()
    {
        var mockSession1 = new Mock<ITerminalSession>();
        var mockSession2 = new Mock<ITerminalSession>();

        _sessionServiceMock
            .Setup(s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession1.Object);
        _sessionServiceMock
            .Setup(s => s.CreateSessionAsync("Debian", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession2.Object);

        await _sut.OpenTabAsync("Ubuntu");
        var ubuntu = _sut.ActiveTab;
        await _sut.OpenTabAsync("Debian");

        _sut.ActivateTab(ubuntu);

        _sut.ActiveTab.Should().Be(ubuntu);
        ubuntu!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ActivateTab_RaisesActiveTabChangedEvent()
    {
        var mockSession = new Mock<ITerminalSession>();
        _sessionServiceMock
            .Setup(s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession.Object);

        await _sut.OpenTabAsync("Ubuntu");

        TerminalTabViewModel? changedTab = null;
        _sut.ActiveTabChanged += tab => changedTab = tab;

        _sut.ActivateTab(null);

        changedTab.Should().BeNull();
    }

    #endregion

    #region NextTab/PreviousTab Tests

    [Fact]
    public async Task NextTab_CyclesToNextTab()
    {
        var mockSession1 = new Mock<ITerminalSession>();
        var mockSession2 = new Mock<ITerminalSession>();

        _sessionServiceMock
            .Setup(s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession1.Object);
        _sessionServiceMock
            .Setup(s => s.CreateSessionAsync("Debian", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession2.Object);

        await _sut.OpenTabAsync("Ubuntu");
        var ubuntu = _sut.Tabs[0];
        await _sut.OpenTabAsync("Debian");

        _sut.ActivateTab(ubuntu);
        _sut.NextTabCommand.Execute(null);

        _sut.ActiveTab!.DistributionName.Should().Be("Debian");
    }

    [Fact]
    public async Task NextTab_WrapsAround()
    {
        var mockSession1 = new Mock<ITerminalSession>();
        var mockSession2 = new Mock<ITerminalSession>();

        _sessionServiceMock
            .Setup(s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession1.Object);
        _sessionServiceMock
            .Setup(s => s.CreateSessionAsync("Debian", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession2.Object);

        await _sut.OpenTabAsync("Ubuntu");
        await _sut.OpenTabAsync("Debian");

        // Active is Debian (last added), next should wrap to Ubuntu
        _sut.NextTabCommand.Execute(null);

        _sut.ActiveTab!.DistributionName.Should().Be("Ubuntu");
    }

    [Fact]
    public async Task PreviousTab_CyclesToPreviousTab()
    {
        var mockSession1 = new Mock<ITerminalSession>();
        var mockSession2 = new Mock<ITerminalSession>();

        _sessionServiceMock
            .Setup(s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession1.Object);
        _sessionServiceMock
            .Setup(s => s.CreateSessionAsync("Debian", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession2.Object);

        await _sut.OpenTabAsync("Ubuntu");
        await _sut.OpenTabAsync("Debian");

        // Active is Debian, previous should go to Ubuntu
        _sut.PreviousTabCommand.Execute(null);

        _sut.ActiveTab!.DistributionName.Should().Be("Ubuntu");
    }

    #endregion

    #region ActivateTabByIndex Tests

    [Fact]
    public async Task ActivateTabByIndex_WithValidIndex_ActivatesCorrectTab()
    {
        var mockSession1 = new Mock<ITerminalSession>();
        var mockSession2 = new Mock<ITerminalSession>();

        _sessionServiceMock
            .Setup(s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession1.Object);
        _sessionServiceMock
            .Setup(s => s.CreateSessionAsync("Debian", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession2.Object);

        await _sut.OpenTabAsync("Ubuntu");
        await _sut.OpenTabAsync("Debian");

        _sut.ActivateTabByIndexCommand.Execute(1); // 1-based index

        _sut.ActiveTab!.DistributionName.Should().Be("Ubuntu");
    }

    [Fact]
    public async Task ActivateTabByIndex_WithInvalidIndex_DoesNothing()
    {
        var mockSession = new Mock<ITerminalSession>();
        _sessionServiceMock
            .Setup(s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession.Object);

        await _sut.OpenTabAsync("Ubuntu");
        var currentTab = _sut.ActiveTab;

        _sut.ActivateTabByIndexCommand.Execute(99);

        _sut.ActiveTab.Should().Be(currentTab);
    }

    #endregion

    #region CloseAllTabsAsync Tests

    [Fact]
    public async Task CloseAllTabsAsync_ClosesAllTabs()
    {
        var mockSession1 = new Mock<ITerminalSession>();
        var mockSession2 = new Mock<ITerminalSession>();

        _sessionServiceMock
            .Setup(s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession1.Object);
        _sessionServiceMock
            .Setup(s => s.CreateSessionAsync("Debian", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession2.Object);

        await _sut.OpenTabAsync("Ubuntu");
        await _sut.OpenTabAsync("Debian");

        await _sut.CloseAllTabsAsync();

        _sut.Tabs.Should().BeEmpty();
        _sut.ActiveTab.Should().BeNull();
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_ClosesAllTabs()
    {
        var mockSession = new Mock<ITerminalSession>();
        _sessionServiceMock
            .Setup(s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession.Object);

        await _sut.OpenTabAsync("Ubuntu");
        await _sut.DisposeAsync();

        _sut.Tabs.Should().BeEmpty();
        mockSession.Verify(s => s.Terminate(), Times.Once);
    }

    #endregion
}
