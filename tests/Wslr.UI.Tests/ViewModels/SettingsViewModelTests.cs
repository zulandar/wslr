using Wslr.UI.Services;
using Wslr.UI.ViewModels;

namespace Wslr.UI.Tests.ViewModels;

public class SettingsViewModelTests
{
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<IStartupService> _startupServiceMock;
    private readonly Mock<ILoggingService> _loggingServiceMock;
    private readonly GlobalWslSettingsViewModel _globalWslSettingsVm;
    private readonly DistroSettingsViewModel _distroSettingsVm;
    private readonly TemplateListViewModel _templateListVm;
    private readonly ProfileListViewModel _profileListVm;

    public SettingsViewModelTests()
    {
        _settingsServiceMock = new Mock<ISettingsService>();
        _startupServiceMock = new Mock<IStartupService>();
        _loggingServiceMock = new Mock<ILoggingService>();

        // Create child VMs with mocked dependencies
        var configService = new Mock<Wslr.Core.Interfaces.IWslConfigService>().Object;
        var distroConfigService = new Mock<Wslr.Core.Interfaces.IWslDistroConfigService>().Object;
        var wslService = new Mock<Wslr.Core.Interfaces.IWslService>().Object;
        var dialogService = new Mock<IDialogService>().Object;
        var templateService = new Mock<Wslr.Core.Interfaces.IConfigurationTemplateService>().Object;
        var profileService = new Mock<Wslr.Core.Interfaces.IConfigurationProfileService>().Object;

        _globalWslSettingsVm = new GlobalWslSettingsViewModel(configService, dialogService);
        _distroSettingsVm = new DistroSettingsViewModel(distroConfigService, wslService, dialogService);
        _templateListVm = new TemplateListViewModel(
            templateService, wslService, dialogService,
            new Mock<Microsoft.Extensions.Logging.ILogger<TemplateListViewModel>>().Object);
        _profileListVm = new ProfileListViewModel(
            profileService, wslService, dialogService,
            new Mock<Microsoft.Extensions.Logging.ILogger<ProfileListViewModel>>().Object);
    }

    private SettingsViewModel CreateViewModel()
    {
        return new SettingsViewModel(
            _settingsServiceMock.Object,
            _startupServiceMock.Object,
            _loggingServiceMock.Object,
            _globalWslSettingsVm,
            _distroSettingsVm,
            _templateListVm,
            _profileListVm);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullSettingsService_ThrowsArgumentNullException()
    {
        var act = () => new SettingsViewModel(
            null!,
            _startupServiceMock.Object,
            _loggingServiceMock.Object,
            _globalWslSettingsVm,
            _distroSettingsVm,
            _templateListVm,
            _profileListVm);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("settingsService");
    }

    [Fact]
    public void Constructor_WithNullStartupService_ThrowsArgumentNullException()
    {
        var act = () => new SettingsViewModel(
            _settingsServiceMock.Object,
            null!,
            _loggingServiceMock.Object,
            _globalWslSettingsVm,
            _distroSettingsVm,
            _templateListVm,
            _profileListVm);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("startupService");
    }

    [Fact]
    public void Constructor_LoadsSettings()
    {
        _settingsServiceMock.Setup(s => s.Get(SettingKeys.MinimizeToTrayOnClose, It.IsAny<bool>())).Returns(false);
        _settingsServiceMock.Setup(s => s.Get(SettingKeys.StartMinimized, It.IsAny<bool>())).Returns(true);
        _settingsServiceMock.Setup(s => s.Get(SettingKeys.ShowNotifications, It.IsAny<bool>())).Returns(false);
        _settingsServiceMock.Setup(s => s.Get(SettingKeys.AutoRefreshEnabled, It.IsAny<bool>())).Returns(false);
        _settingsServiceMock.Setup(s => s.Get(SettingKeys.AutoRefreshIntervalSeconds, It.IsAny<int>())).Returns(10);
        _settingsServiceMock.Setup(s => s.Get(SettingKeys.DebugLoggingEnabled, It.IsAny<bool>())).Returns(true);
        _startupServiceMock.Setup(s => s.IsStartupEnabled()).Returns(true);

        var vm = CreateViewModel();

        vm.MinimizeToTrayOnClose.Should().BeFalse();
        vm.StartMinimized.Should().BeTrue();
        vm.ShowNotifications.Should().BeFalse();
        vm.AutoRefreshEnabled.Should().BeFalse();
        vm.AutoRefreshIntervalSeconds.Should().Be(10);
        vm.DebugLoggingEnabled.Should().BeTrue();
        vm.StartWithWindows.Should().BeTrue();
    }

    #endregion

    #region Property Change Tests

    [Fact]
    public void MinimizeToTrayOnClose_WhenChanged_SavesSettings()
    {
        var vm = CreateViewModel();

        vm.MinimizeToTrayOnClose = true;

        _settingsServiceMock.Verify(s => s.Set(SettingKeys.MinimizeToTrayOnClose, true), Times.Once);
        _settingsServiceMock.Verify(s => s.Save(), Times.Once);
    }

    [Fact]
    public void StartMinimized_WhenChanged_SavesSettings()
    {
        var vm = CreateViewModel();

        vm.StartMinimized = true;

        _settingsServiceMock.Verify(s => s.Set(SettingKeys.StartMinimized, true), Times.Once);
        _settingsServiceMock.Verify(s => s.Save(), Times.Once);
    }

    [Fact]
    public void StartWithWindows_WhenSetToTrue_EnablesStartup()
    {
        var vm = CreateViewModel();

        vm.StartWithWindows = true;

        _startupServiceMock.Verify(s => s.EnableStartup(), Times.Once);
    }

    [Fact]
    public void StartWithWindows_WhenSetToFalse_DisablesStartup()
    {
        _startupServiceMock.Setup(s => s.IsStartupEnabled()).Returns(true);
        var vm = CreateViewModel();

        vm.StartWithWindows = false;

        _startupServiceMock.Verify(s => s.DisableStartup(), Times.Once);
    }

    [Fact]
    public void ShowNotifications_WhenChanged_SavesSettings()
    {
        // Setup initial value as true so setting to false triggers change
        _settingsServiceMock.Setup(s => s.Get(SettingKeys.ShowNotifications, It.IsAny<bool>())).Returns(true);
        var vm = CreateViewModel();

        vm.ShowNotifications = false;

        _settingsServiceMock.Verify(s => s.Set(SettingKeys.ShowNotifications, false), Times.Once);
        _settingsServiceMock.Verify(s => s.Save(), Times.AtLeastOnce);
    }

    [Fact]
    public void AutoRefreshEnabled_WhenChanged_SavesSettings()
    {
        // Setup initial value as true so setting to false triggers change
        _settingsServiceMock.Setup(s => s.Get(SettingKeys.AutoRefreshEnabled, It.IsAny<bool>())).Returns(true);
        var vm = CreateViewModel();

        vm.AutoRefreshEnabled = false;

        _settingsServiceMock.Verify(s => s.Set(SettingKeys.AutoRefreshEnabled, false), Times.Once);
        _settingsServiceMock.Verify(s => s.Save(), Times.AtLeastOnce);
    }

    [Fact]
    public void AutoRefreshIntervalSeconds_WhenChanged_SavesSettings()
    {
        var vm = CreateViewModel();

        vm.AutoRefreshIntervalSeconds = 15;

        _settingsServiceMock.Verify(s => s.Set(SettingKeys.AutoRefreshIntervalSeconds, 15), Times.Once);
        _settingsServiceMock.Verify(s => s.Save(), Times.AtLeastOnce);
    }

    [Fact]
    public void DebugLoggingEnabled_WhenChanged_SavesSettingsAndUpdatesLogging()
    {
        var vm = CreateViewModel();

        vm.DebugLoggingEnabled = true;

        _settingsServiceMock.Verify(s => s.Set(SettingKeys.DebugLoggingEnabled, true), Times.Once);
        _settingsServiceMock.Verify(s => s.Save(), Times.AtLeastOnce);
        _loggingServiceMock.Verify(s => s.SetDebugLogging(true), Times.Once);
    }

    #endregion

    #region Command Tests

    [Fact]
    public void OpenLogFolderCommand_OpensLogFolder()
    {
        var vm = CreateViewModel();

        vm.OpenLogFolderCommand.Execute(null);

        _loggingServiceMock.Verify(s => s.OpenLogFolder(), Times.Once);
    }

    #endregion

    #region Child ViewModel Tests

    [Fact]
    public void GlobalWslSettingsViewModel_IsExposed()
    {
        var vm = CreateViewModel();

        vm.GlobalWslSettingsViewModel.Should().Be(_globalWslSettingsVm);
    }

    [Fact]
    public void DistroSettingsViewModel_IsExposed()
    {
        var vm = CreateViewModel();

        vm.DistroSettingsViewModel.Should().Be(_distroSettingsVm);
    }

    [Fact]
    public void TemplateListViewModel_IsExposed()
    {
        var vm = CreateViewModel();

        vm.TemplateListViewModel.Should().Be(_templateListVm);
    }

    [Fact]
    public void ProfileListViewModel_IsExposed()
    {
        var vm = CreateViewModel();

        vm.ProfileListViewModel.Should().Be(_profileListVm);
    }

    #endregion
}
