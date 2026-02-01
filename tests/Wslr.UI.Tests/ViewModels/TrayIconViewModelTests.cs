using Wslr.Core.Interfaces;
using Wslr.Core.Models;
using Wslr.UI.Services;
using Wslr.UI.ViewModels;

namespace Wslr.UI.Tests.ViewModels;

public class TrayIconViewModelTests
{
    private readonly Mock<IWslService> _wslServiceMock;
    private readonly Mock<INavigationService> _navigationServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IDistributionMonitorService> _monitorServiceMock;
    private readonly Mock<IConfigurationProfileService> _profileServiceMock;

    public TrayIconViewModelTests()
    {
        _wslServiceMock = new Mock<IWslService>();
        _navigationServiceMock = new Mock<INavigationService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _monitorServiceMock = new Mock<IDistributionMonitorService>();
        _profileServiceMock = new Mock<IConfigurationProfileService>();

        _profileServiceMock.Setup(s => s.GetAllProfilesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConfigurationProfile>());
    }

    private TrayIconViewModel CreateViewModel()
    {
        return new TrayIconViewModel(
            _wslServiceMock.Object,
            _navigationServiceMock.Object,
            _notificationServiceMock.Object,
            _monitorServiceMock.Object,
            _profileServiceMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullWslService_ThrowsArgumentNullException()
    {
        var act = () => new TrayIconViewModel(
            null!,
            _navigationServiceMock.Object,
            _notificationServiceMock.Object,
            _monitorServiceMock.Object,
            _profileServiceMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("wslService");
    }

    [Fact]
    public void Constructor_InitializesWithEmptyCollections()
    {
        var vm = CreateViewModel();

        vm.Distributions.Should().BeEmpty();
        vm.Profiles.Should().BeEmpty();
        vm.IsStateChangeNotificationsEnabled.Should().BeTrue();
    }

    #endregion

    #region RefreshDistributionsAsync Tests

    [Fact]
    public async Task RefreshDistributionsAsync_CallsMonitorRefresh()
    {
        var vm = CreateViewModel();

        await vm.RefreshDistributionsCommand.ExecuteAsync(null);

        _monitorServiceMock.Verify(s => s.RefreshAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshDistributionsAsync_ShowsErrorNotification_OnFailure()
    {
        _monitorServiceMock.Setup(s => s.RefreshAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Refresh failed"));

        var vm = CreateViewModel();

        await vm.RefreshDistributionsCommand.ExecuteAsync(null);

        _notificationServiceMock.Verify(s => s.ShowError("Error", It.Is<string>(m => m.Contains("Refresh failed"))), Times.Once);
    }

    #endregion

    #region StartDistributionAsync Tests

    [Fact]
    public async Task StartDistributionAsync_StartsDistributionAndRefreshes()
    {
        var vm = CreateViewModel();

        await vm.StartDistributionCommand.ExecuteAsync("Ubuntu");

        _wslServiceMock.Verify(s => s.StartDistributionAsync("Ubuntu", It.IsAny<CancellationToken>()), Times.Once);
        _monitorServiceMock.Verify(s => s.RefreshAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationServiceMock.Verify(s => s.ShowSuccess("Started", It.Is<string>(m => m.Contains("Ubuntu"))), Times.Once);
    }

    [Fact]
    public async Task StartDistributionAsync_ShowsError_OnFailure()
    {
        _wslServiceMock.Setup(s => s.StartDistributionAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Start failed"));

        var vm = CreateViewModel();

        await vm.StartDistributionCommand.ExecuteAsync("Ubuntu");

        _notificationServiceMock.Verify(s => s.ShowError("Error", It.Is<string>(m => m.Contains("Start failed"))), Times.Once);
    }

    #endregion

    #region StopDistributionAsync Tests

    [Fact]
    public async Task StopDistributionAsync_StopsDistributionAndRefreshes()
    {
        var vm = CreateViewModel();

        await vm.StopDistributionCommand.ExecuteAsync("Ubuntu");

        _wslServiceMock.Verify(s => s.TerminateDistributionAsync("Ubuntu", It.IsAny<CancellationToken>()), Times.Once);
        _monitorServiceMock.Verify(s => s.RefreshAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationServiceMock.Verify(s => s.ShowSuccess("Stopped", It.Is<string>(m => m.Contains("Ubuntu"))), Times.Once);
    }

    #endregion

    #region ShutdownAllAsync Tests

    [Fact]
    public async Task ShutdownAllAsync_ShutdownsWslAndRefreshes()
    {
        var vm = CreateViewModel();

        await vm.ShutdownAllCommand.ExecuteAsync(null);

        _wslServiceMock.Verify(s => s.ShutdownAsync(It.IsAny<CancellationToken>()), Times.Once);
        _monitorServiceMock.Verify(s => s.RefreshAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationServiceMock.Verify(s => s.ShowSuccess("Shutdown", It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region ShowMainWindow Tests

    [Fact]
    public void ShowMainWindowCommand_ShowsMainWindow()
    {
        var vm = CreateViewModel();

        vm.ShowMainWindowCommand.Execute(null);

        _navigationServiceMock.Verify(s => s.ShowMainWindow(), Times.Once);
    }

    #endregion

    #region Exit Tests

    [Fact]
    public void ExitCommand_ExitsApplication()
    {
        var vm = CreateViewModel();

        vm.ExitCommand.Execute(null);

        _navigationServiceMock.Verify(s => s.ExitApplication(), Times.Once);
    }

    #endregion

    #region State Change Notification Tests

    [Fact]
    public void OnDistributionStateChanged_WhenNotificationsEnabled_ShowsNotification()
    {
        var vm = CreateViewModel();
        vm.IsStateChangeNotificationsEnabled = true;

        // Raise the event from the monitor service
        _monitorServiceMock.Raise(m => m.DistributionStateChanged += null,
            _monitorServiceMock.Object,
            new DistributionStateChangedEventArgs
            {
                DistributionName = "Ubuntu",
                OldState = DistributionState.Stopped,
                NewState = DistributionState.Running
            });

        _notificationServiceMock.Verify(
            s => s.ShowInfo("Distribution Started", It.Is<string>(m => m.Contains("Ubuntu"))),
            Times.Once);
    }

    [Fact]
    public void OnDistributionStateChanged_WhenNotificationsDisabled_DoesNotShowNotification()
    {
        var vm = CreateViewModel();
        vm.IsStateChangeNotificationsEnabled = false;

        _monitorServiceMock.Raise(m => m.DistributionStateChanged += null,
            _monitorServiceMock.Object,
            new DistributionStateChangedEventArgs
            {
                DistributionName = "Ubuntu",
                OldState = DistributionState.Stopped,
                NewState = DistributionState.Running
            });

        _notificationServiceMock.Verify(
            s => s.ShowInfo(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public void OnDistributionStateChanged_WhenDistributionAdded_ShowsAddedNotification()
    {
        var vm = CreateViewModel();

        // WasAdded is computed: OldState is null and NewState is not null
        _monitorServiceMock.Raise(m => m.DistributionStateChanged += null,
            _monitorServiceMock.Object,
            new DistributionStateChangedEventArgs
            {
                DistributionName = "NewDistro",
                OldState = null,
                NewState = DistributionState.Stopped
            });

        _notificationServiceMock.Verify(
            s => s.ShowInfo("Distribution Added", It.Is<string>(m => m.Contains("NewDistro"))),
            Times.Once);
    }

    [Fact]
    public void OnDistributionStateChanged_WhenDistributionRemoved_ShowsRemovedNotification()
    {
        var vm = CreateViewModel();

        // WasRemoved is computed: OldState is not null and NewState is null
        _monitorServiceMock.Raise(m => m.DistributionStateChanged += null,
            _monitorServiceMock.Object,
            new DistributionStateChangedEventArgs
            {
                DistributionName = "OldDistro",
                OldState = DistributionState.Stopped,
                NewState = null
            });

        _notificationServiceMock.Verify(
            s => s.ShowInfo("Distribution Removed", It.Is<string>(m => m.Contains("OldDistro"))),
            Times.Once);
    }

    #endregion

    #region Profile Management Tests

    [Fact]
    public async Task SwitchProfileAsync_WhenProfileNotFound_ShowsError()
    {
        _profileServiceMock.Setup(s => s.GetProfileAsync("invalid", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConfigurationProfile?)null);

        var vm = CreateViewModel();

        await vm.SwitchProfileCommand.ExecuteAsync("invalid");

        _notificationServiceMock.Verify(s => s.ShowError("Error", "Profile not found."), Times.Once);
    }

    [Fact]
    public async Task SwitchProfileAsync_WhenSuccessful_ShowsSuccessNotification()
    {
        var profile = new ConfigurationProfile { Id = "test", Name = "Test Profile" };
        _profileServiceMock.Setup(s => s.GetProfileAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _profileServiceMock.Setup(s => s.SwitchToProfileAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfileSwitchResult { Success = true });

        var vm = CreateViewModel();

        await vm.SwitchProfileCommand.ExecuteAsync("test");

        _notificationServiceMock.Verify(
            s => s.ShowSuccess("Profile Switched", It.Is<string>(m => m.Contains("Test Profile"))),
            Times.Once);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        var vm = CreateViewModel();

        vm.Dispose();

        // Verify that subsequent events don't cause issues
        // (If unsubscription failed, this would throw or behave unexpectedly)
        _monitorServiceMock.Raise(m => m.DistributionsRefreshed += null, _monitorServiceMock.Object, EventArgs.Empty);
    }

    #endregion
}
