using System.IO;
using Wslr.Core.Interfaces;
using Wslr.Core.Models;
using Wslr.UI.Services;
using Wslr.UI.ViewModels;

namespace Wslr.UI.Tests.ViewModels;

public class GlobalWslSettingsViewModelTests
{
    private readonly Mock<IWslConfigService> _configServiceMock;
    private readonly Mock<IDialogService> _dialogServiceMock;

    public GlobalWslSettingsViewModelTests()
    {
        _configServiceMock = new Mock<IWslConfigService>();
        _dialogServiceMock = new Mock<IDialogService>();
    }

    private GlobalWslSettingsViewModel CreateViewModel()
    {
        return new GlobalWslSettingsViewModel(
            _configServiceMock.Object,
            _dialogServiceMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullConfigService_ThrowsArgumentNullException()
    {
        var act = () => new GlobalWslSettingsViewModel(null!, _dialogServiceMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configService");
    }

    [Fact]
    public void Constructor_WithNullDialogService_ThrowsArgumentNullException()
    {
        var act = () => new GlobalWslSettingsViewModel(_configServiceMock.Object, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("dialogService");
    }

    [Fact]
    public void Constructor_InitializesWithDefaultState()
    {
        var vm = CreateViewModel();

        vm.IsLoading.Should().BeFalse();
        vm.IsSaving.Should().BeFalse();
        vm.IsDirty.Should().BeFalse();
        vm.ErrorMessage.Should().BeNull();
        vm.SuccessMessage.Should().BeNull();
    }

    #endregion

    #region LoadAsync Tests

    [Fact]
    public async Task LoadAsync_LoadsConfigurationFromService()
    {
        var config = new WslConfig
        {
            Wsl2 = new Wsl2Settings
            {
                Memory = "8GB",
                Processors = 4,
                LocalhostForwarding = true
            }
        };
        _configServiceMock.Setup(s => s.ReadConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        _configServiceMock.SetupGet(s => s.ConfigExists).Returns(true);

        var vm = CreateViewModel();
        await vm.LoadAsync();

        vm.Memory.Should().Be("8GB");
        vm.Processors.Should().Be(4);
        vm.LocalhostForwarding.Should().BeTrue();
        vm.ConfigExists.Should().BeTrue();
        vm.IsDirty.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_SetsErrorMessage_WhenLoadFails()
    {
        _configServiceMock.Setup(s => s.ReadConfigAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("File not accessible"));

        var vm = CreateViewModel();
        await vm.LoadAsync();

        vm.ErrorMessage.Should().Contain("Failed to load configuration");
        vm.IsLoading.Should().BeFalse();
    }

    #endregion

    #region SaveAsync Tests

    [Fact]
    public async Task SaveAsync_WhenNotDirty_DoesNothing()
    {
        var vm = CreateViewModel();

        await vm.SaveAsync();

        _configServiceMock.Verify(s => s.WriteConfigAsync(It.IsAny<WslConfig>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_WhenDirty_SavesConfiguration()
    {
        _configServiceMock.Setup(s => s.ReadConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WslConfig());
        _configServiceMock.Setup(s => s.Validate(It.IsAny<WslConfig>()))
            .Returns(WslConfigValidationResult.Success);

        var vm = CreateViewModel();
        await vm.LoadAsync();
        vm.Memory = "16GB"; // This should set IsDirty

        await vm.SaveAsync();

        _configServiceMock.Verify(s => s.CreateBackupAsync(It.IsAny<CancellationToken>()), Times.Once);
        _configServiceMock.Verify(s => s.WriteConfigAsync(It.IsAny<WslConfig>(), It.IsAny<CancellationToken>()), Times.Once);
        vm.IsDirty.Should().BeFalse();
        vm.SuccessMessage.Should().Contain("Settings saved");
    }

    [Fact]
    public async Task SaveAsync_WhenValidationFails_SetsErrorMessage()
    {
        _configServiceMock.Setup(s => s.ReadConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WslConfig());
        _configServiceMock.Setup(s => s.Validate(It.IsAny<WslConfig>()))
            .Returns(WslConfigValidationResult.Failure(
                new WslConfigValidationError { Message = "Invalid memory value" }));

        var vm = CreateViewModel();
        await vm.LoadAsync();
        vm.Memory = "invalid"; // Trigger dirty

        await vm.SaveAsync();

        vm.ErrorMessage.Should().Contain("Invalid memory value");
        _configServiceMock.Verify(s => s.WriteConfigAsync(It.IsAny<WslConfig>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public async Task Cancel_RevertsToOriginalValues()
    {
        var config = new WslConfig
        {
            Wsl2 = new Wsl2Settings { Memory = "8GB" }
        };
        _configServiceMock.Setup(s => s.ReadConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var vm = CreateViewModel();
        await vm.LoadAsync();
        vm.Memory = "16GB"; // Modify

        vm.Cancel();

        vm.Memory.Should().Be("8GB");
        vm.IsDirty.Should().BeFalse();
    }

    #endregion

    #region ResetToDefaultsAsync Tests

    [Fact]
    public async Task ResetToDefaultsAsync_WhenConfirmed_ResetsAllValues()
    {
        _configServiceMock.Setup(s => s.ReadConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WslConfig { Wsl2 = new Wsl2Settings { Memory = "16GB" } });
        _dialogServiceMock.Setup(s => s.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var vm = CreateViewModel();
        await vm.LoadAsync();

        await vm.ResetToDefaultsAsync();

        vm.Memory.Should().BeNull(); // Default is null
        vm.IsDirty.Should().BeTrue();
    }

    [Fact]
    public async Task ResetToDefaultsAsync_WhenCancelled_DoesNothing()
    {
        _configServiceMock.Setup(s => s.ReadConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WslConfig { Wsl2 = new Wsl2Settings { Memory = "16GB" } });
        _dialogServiceMock.Setup(s => s.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var vm = CreateViewModel();
        await vm.LoadAsync();

        await vm.ResetToDefaultsAsync();

        vm.Memory.Should().Be("16GB");
    }

    #endregion

    #region Dirty Tracking Tests

    [Fact]
    public async Task PropertyChanges_SetIsDirty()
    {
        _configServiceMock.Setup(s => s.ReadConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WslConfig());

        var vm = CreateViewModel();
        await vm.LoadAsync();

        vm.Memory = "8GB";
        vm.IsDirty.Should().BeTrue();
    }

    [Fact]
    public async Task PropertyChanges_DuringLoad_DoNotSetDirty()
    {
        _configServiceMock.Setup(s => s.ReadConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WslConfig { Wsl2 = new Wsl2Settings { Memory = "8GB" } });

        var vm = CreateViewModel();
        await vm.LoadAsync();

        vm.IsDirty.Should().BeFalse();
    }

    #endregion

    #region Computed Properties Tests

    [Fact]
    public void MaxProcessors_ReturnsEnvironmentProcessorCount()
    {
        var vm = CreateViewModel();

        vm.MaxProcessors.Should().Be(Environment.ProcessorCount);
    }

    [Fact]
    public void NetworkingModes_ContainsExpectedOptions()
    {
        var vm = CreateViewModel();

        vm.NetworkingModes.Should().Contain("nat");
        vm.NetworkingModes.Should().Contain("mirrored");
    }

    [Fact]
    public void AutoMemoryReclaimOptions_ContainsExpectedOptions()
    {
        var vm = CreateViewModel();

        vm.AutoMemoryReclaimOptions.Should().Contain("disabled");
        vm.AutoMemoryReclaimOptions.Should().Contain("gradual");
        vm.AutoMemoryReclaimOptions.Should().Contain("dropcache");
    }

    #endregion
}
