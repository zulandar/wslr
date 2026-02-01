using Wslr.Core.Interfaces;
using Wslr.Core.Models;
using Wslr.UI.Services;
using Wslr.UI.ViewModels;
using MockFactoryHelper = Wslr.UI.Tests.Helpers.MockFactory;

namespace Wslr.UI.Tests.ViewModels;

public class DistroSettingsViewModelTests
{
    private readonly Mock<IWslDistroConfigService> _configServiceMock;
    private readonly Mock<IWslService> _wslServiceMock;
    private readonly Mock<IDialogService> _dialogServiceMock;

    public DistroSettingsViewModelTests()
    {
        _configServiceMock = new Mock<IWslDistroConfigService>();
        _wslServiceMock = new Mock<IWslService>();
        _dialogServiceMock = new Mock<IDialogService>();
    }

    private DistroSettingsViewModel CreateViewModel()
    {
        return new DistroSettingsViewModel(
            _configServiceMock.Object,
            _wslServiceMock.Object,
            _dialogServiceMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullConfigService_ThrowsArgumentNullException()
    {
        var act = () => new DistroSettingsViewModel(null!, _wslServiceMock.Object, _dialogServiceMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configService");
    }

    [Fact]
    public void Constructor_WithNullWslService_ThrowsArgumentNullException()
    {
        var act = () => new DistroSettingsViewModel(_configServiceMock.Object, null!, _dialogServiceMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("wslService");
    }

    [Fact]
    public void Constructor_InitializesWithDefaultState()
    {
        var vm = CreateViewModel();

        vm.IsLoading.Should().BeFalse();
        vm.IsSaving.Should().BeFalse();
        vm.IsDirty.Should().BeFalse();
        vm.SelectedDistribution.Should().BeNull();
        vm.Distributions.Should().BeEmpty();
    }

    #endregion

    #region LoadDistributionsAsync Tests

    [Fact]
    public async Task LoadDistributionsAsync_LoadsDistributionList()
    {
        _wslServiceMock.Setup(s => s.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>
            {
                MockFactoryHelper.CreateDistribution("Ubuntu"),
                MockFactoryHelper.CreateDistribution("Debian"),
                MockFactoryHelper.CreateDistribution("Alpine")
            });

        var vm = CreateViewModel();
        await vm.LoadDistributionsAsync();

        vm.Distributions.Should().HaveCount(3);
        vm.Distributions.Should().Contain("Ubuntu");
        vm.Distributions.Should().Contain("Debian");
        vm.Distributions.Should().Contain("Alpine");
    }

    [Fact]
    public async Task LoadDistributionsAsync_SelectsFirstDistribution()
    {
        _wslServiceMock.Setup(s => s.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>
            {
                MockFactoryHelper.CreateDistribution("Debian"),
                MockFactoryHelper.CreateDistribution("Ubuntu")
            });

        var vm = CreateViewModel();
        await vm.LoadDistributionsAsync();

        // Distributions are sorted by name
        vm.SelectedDistribution.Should().Be("Debian");
    }

    [Fact]
    public async Task LoadDistributionsAsync_SetsErrorMessage_OnFailure()
    {
        _wslServiceMock.Setup(s => s.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("WSL error"));

        var vm = CreateViewModel();
        await vm.LoadDistributionsAsync();

        vm.ErrorMessage.Should().Contain("Failed to load distributions");
    }

    #endregion

    #region LoadConfigAsync Tests

    [Fact]
    public async Task LoadConfigAsync_LoadsConfigForSelectedDistribution()
    {
        var config = new WslDistroConfig
        {
            Automount = new AutomountSettings { Enabled = true, Root = "/mnt/" },
            Boot = new BootSettings { Systemd = true },
            Interop = new InteropSettings { Enabled = true, AppendWindowsPath = false }
        };

        _configServiceMock.Setup(s => s.ReadConfigAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        _configServiceMock.Setup(s => s.ConfigExists("Ubuntu")).Returns(true);

        var vm = CreateViewModel();
        vm.SelectedDistribution = "Ubuntu";
        await vm.LoadConfigAsync();

        vm.AutomountEnabled.Should().BeTrue();
        vm.AutomountRoot.Should().Be("/mnt/");
        vm.SystemdEnabled.Should().BeTrue();
        vm.InteropEnabled.Should().BeTrue();
        vm.AppendWindowsPath.Should().BeFalse();
        vm.ConfigExists.Should().BeTrue();
    }

    [Fact]
    public async Task LoadConfigAsync_WhenNoDistributionSelected_DoesNothing()
    {
        var vm = CreateViewModel();
        vm.SelectedDistribution = null;

        await vm.LoadConfigAsync();

        _configServiceMock.Verify(s => s.ReadConfigAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region SaveAsync Tests

    [Fact]
    public async Task SaveAsync_WhenNotDirty_DoesNothing()
    {
        var vm = CreateViewModel();
        vm.SelectedDistribution = "Ubuntu";

        await vm.SaveAsync();

        _configServiceMock.Verify(s => s.WriteConfigAsync(It.IsAny<string>(), It.IsAny<WslDistroConfig>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_WhenNoDistributionSelected_DoesNothing()
    {
        var vm = CreateViewModel();
        vm.SelectedDistribution = null;

        await vm.SaveAsync();

        _configServiceMock.Verify(s => s.WriteConfigAsync(It.IsAny<string>(), It.IsAny<WslDistroConfig>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_WhenDirty_SavesConfiguration()
    {
        _configServiceMock.Setup(s => s.ReadConfigAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WslDistroConfig());
        _configServiceMock.Setup(s => s.Validate(It.IsAny<WslDistroConfig>()))
            .Returns(WslDistroConfigValidationResult.Success);
        _configServiceMock.Setup(s => s.ConfigExists("Ubuntu")).Returns(true);

        var vm = CreateViewModel();
        vm.SelectedDistribution = "Ubuntu";
        await vm.LoadConfigAsync();
        vm.SystemdEnabled = false; // Trigger dirty

        await vm.SaveAsync();

        _configServiceMock.Verify(s => s.CreateBackupAsync("Ubuntu", It.IsAny<CancellationToken>()), Times.Once);
        _configServiceMock.Verify(s => s.WriteConfigAsync("Ubuntu", It.IsAny<WslDistroConfig>(), It.IsAny<CancellationToken>()), Times.Once);
        vm.IsDirty.Should().BeFalse();
        vm.SuccessMessage.Should().Contain("Settings saved");
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public async Task Cancel_RevertsToOriginalValues()
    {
        var config = new WslDistroConfig
        {
            Boot = new BootSettings { Systemd = true }
        };
        _configServiceMock.Setup(s => s.ReadConfigAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var vm = CreateViewModel();
        vm.SelectedDistribution = "Ubuntu";
        await vm.LoadConfigAsync();
        vm.SystemdEnabled = false; // Modify

        vm.Cancel();

        vm.SystemdEnabled.Should().BeTrue();
        vm.IsDirty.Should().BeFalse();
    }

    #endregion

    #region ResetToDefaultsAsync Tests

    [Fact]
    public async Task ResetToDefaultsAsync_WhenConfirmed_ResetsAllValues()
    {
        _configServiceMock.Setup(s => s.ReadConfigAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WslDistroConfig { Boot = new BootSettings { Systemd = false } });
        _dialogServiceMock.Setup(s => s.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var vm = CreateViewModel();
        vm.SelectedDistribution = "Ubuntu";
        await vm.LoadConfigAsync();

        await vm.ResetToDefaultsAsync();

        vm.SystemdEnabled.Should().BeTrue(); // Default is true
        vm.IsDirty.Should().BeTrue();
    }

    #endregion

    #region SelectedDistribution Change Tests

    [Fact]
    public async Task OnSelectedDistributionChanged_LoadsConfig()
    {
        _configServiceMock.Setup(s => s.ReadConfigAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WslDistroConfig());

        var vm = CreateViewModel();
        vm.SelectedDistribution = "Ubuntu";

        // Give time for the async load to complete
        await Task.Delay(100);

        _configServiceMock.Verify(s => s.ReadConfigAsync("Ubuntu", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ConfigPath Tests

    [Fact]
    public void ConfigPath_WhenDistributionSelected_ReturnsPath()
    {
        _configServiceMock.Setup(s => s.GetConfigPath("Ubuntu")).Returns("/etc/wsl.conf");

        var vm = CreateViewModel();
        vm.SelectedDistribution = "Ubuntu";

        vm.ConfigPath.Should().Be("/etc/wsl.conf");
    }

    [Fact]
    public void ConfigPath_WhenNoDistributionSelected_ReturnsNull()
    {
        var vm = CreateViewModel();
        vm.SelectedDistribution = null;

        vm.ConfigPath.Should().BeNull();
    }

    #endregion
}
