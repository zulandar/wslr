using Wslr.Core.Interfaces;
using Wslr.UI.ViewModels;

namespace Wslr.UI.Tests.ViewModels;

public class TerminalTabViewModelTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_SetsDistributionName()
    {
        var vm = new TerminalTabViewModel("Ubuntu");

        vm.DistributionName.Should().Be("Ubuntu");
    }

    [Fact]
    public void Constructor_SetsTitleToDistributionName()
    {
        var vm = new TerminalTabViewModel("Ubuntu");

        vm.Title.Should().Be("Ubuntu");
    }

    [Fact]
    public void Constructor_GeneratesUniqueId()
    {
        var vm1 = new TerminalTabViewModel("Ubuntu");
        var vm2 = new TerminalTabViewModel("Ubuntu");

        vm1.Id.Should().NotBe(vm2.Id);
        vm1.Id.Should().HaveLength(8);
    }

    [Fact]
    public void Constructor_InitializesWithDisconnectedState()
    {
        var vm = new TerminalTabViewModel("Ubuntu");

        vm.IsConnected.Should().BeFalse();
        vm.IsConnecting.Should().BeFalse();
        vm.IsActive.Should().BeFalse();
        vm.ErrorMessage.Should().BeNull();
        vm.SessionId.Should().BeNull();
    }

    #endregion

    #region ConnectAsync Tests

    [Fact]
    public async Task ConnectAsync_WithValidSession_SetsConnectedState()
    {
        var vm = new TerminalTabViewModel("Ubuntu");
        var mockSession = new Mock<ITerminalSession>();
        mockSession.Setup(s => s.Id).Returns("session123");

        var mockSessionService = new Mock<ITerminalSessionService>();
        mockSessionService
            .Setup(s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession.Object);

        await vm.ConnectAsync(mockSessionService.Object);

        vm.IsConnected.Should().BeTrue();
        vm.IsConnecting.Should().BeFalse();
        vm.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ConnectAsync_WhenSessionCreationFails_SetsErrorMessage()
    {
        var vm = new TerminalTabViewModel("Ubuntu");
        var mockSessionService = new Mock<ITerminalSessionService>();
        mockSessionService
            .Setup(s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection failed"));

        await vm.ConnectAsync(mockSessionService.Object);

        vm.IsConnected.Should().BeFalse();
        vm.IsConnecting.Should().BeFalse();
        vm.ErrorMessage.Should().Contain("Connection failed");
    }

    [Fact]
    public async Task ConnectAsync_WhenAlreadyConnected_DoesNothing()
    {
        var vm = new TerminalTabViewModel("Ubuntu");
        var mockSession = new Mock<ITerminalSession>();
        var mockSessionService = new Mock<ITerminalSessionService>();
        mockSessionService
            .Setup(s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession.Object);

        await vm.ConnectAsync(mockSessionService.Object);
        await vm.ConnectAsync(mockSessionService.Object);

        mockSessionService.Verify(
            s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region DisconnectAsync Tests

    [Fact]
    public async Task DisconnectAsync_WhenConnected_TerminatesSession()
    {
        var vm = new TerminalTabViewModel("Ubuntu");
        var mockSession = new Mock<ITerminalSession>();
        var mockSessionService = new Mock<ITerminalSessionService>();
        mockSessionService
            .Setup(s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession.Object);

        await vm.ConnectAsync(mockSessionService.Object);
        await vm.DisconnectAsync();

        vm.IsConnected.Should().BeFalse();
        mockSession.Verify(s => s.Terminate(), Times.Once);
    }

    [Fact]
    public async Task DisconnectAsync_WhenNotConnected_DoesNothing()
    {
        var vm = new TerminalTabViewModel("Ubuntu");

        await vm.DisconnectAsync();

        vm.IsConnected.Should().BeFalse();
    }

    #endregion

    #region SendInputAsync Tests

    [Fact]
    public async Task SendInputAsync_WhenConnected_WritesToSession()
    {
        var vm = new TerminalTabViewModel("Ubuntu");
        var mockSession = new Mock<ITerminalSession>();
        var mockSessionService = new Mock<ITerminalSessionService>();
        mockSessionService
            .Setup(s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession.Object);

        await vm.ConnectAsync(mockSessionService.Object);
        await vm.SendInputAsync("ls -la");

        mockSession.Verify(s => s.WriteAsync("ls -la", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendInputAsync_WhenNotConnected_DoesNothing()
    {
        var vm = new TerminalTabViewModel("Ubuntu");

        await vm.SendInputAsync("ls -la");

        // No exception should be thrown
    }

    #endregion

    #region ResizeAsync Tests

    [Fact]
    public async Task ResizeAsync_WhenConnected_ResizesSession()
    {
        var vm = new TerminalTabViewModel("Ubuntu");
        var mockSession = new Mock<ITerminalSession>();
        var mockSessionService = new Mock<ITerminalSessionService>();
        mockSessionService
            .Setup(s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession.Object);

        await vm.ConnectAsync(mockSessionService.Object);
        await vm.ResizeAsync(120, 40);

        mockSession.Verify(s => s.ResizeAsync(120, 40, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResizeAsync_WhenNotConnected_DoesNothing()
    {
        var vm = new TerminalTabViewModel("Ubuntu");

        await vm.ResizeAsync(120, 40);

        // No exception should be thrown
    }

    #endregion

    #region Event Tests

    [Fact]
    public void CloseCommand_RaisesCloseRequestedEvent()
    {
        var vm = new TerminalTabViewModel("Ubuntu");
        TerminalTabViewModel? requestedTab = null;
        vm.CloseRequested += tab => requestedTab = tab;

        vm.CloseCommand.Execute(null);

        requestedTab.Should().Be(vm);
    }

    [Fact]
    public void ActivateCommand_RaisesActivateRequestedEvent()
    {
        var vm = new TerminalTabViewModel("Ubuntu");
        TerminalTabViewModel? requestedTab = null;
        vm.ActivateRequested += tab => requestedTab = tab;

        vm.ActivateCommand.Execute(null);

        requestedTab.Should().Be(vm);
    }

    [Fact]
    public async Task OutputReceived_FlushesBufferedOutput_WhenFirstSubscriberAttaches()
    {
        var vm = new TerminalTabViewModel("Ubuntu");
        var mockSession = new Mock<ITerminalSession>();

        // Capture the OutputReceived handler
        Action<string>? sessionOutputHandler = null;
        mockSession.SetupAdd(s => s.OutputReceived += It.IsAny<Action<string>>())
            .Callback<Action<string>>(handler => sessionOutputHandler = handler);

        var mockSessionService = new Mock<ITerminalSessionService>();
        mockSessionService
            .Setup(s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession.Object);

        await vm.ConnectAsync(mockSessionService.Object);

        // Simulate output before subscriber
        sessionOutputHandler?.Invoke("buffered output");

        // Now subscribe and verify buffered output is flushed
        var receivedOutput = new List<string>();
        vm.OutputReceived += output => receivedOutput.Add(output);

        receivedOutput.Should().Contain("buffered output");
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_ClosesSession()
    {
        var vm = new TerminalTabViewModel("Ubuntu");
        var mockSession = new Mock<ITerminalSession>();
        var mockSessionService = new Mock<ITerminalSessionService>();
        mockSessionService
            .Setup(s => s.CreateSessionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSession.Object);

        await vm.ConnectAsync(mockSessionService.Object);
        await vm.DisposeAsync();

        vm.IsConnected.Should().BeFalse();
        mockSession.Verify(s => s.Terminate(), Times.Once);
    }

    #endregion
}
