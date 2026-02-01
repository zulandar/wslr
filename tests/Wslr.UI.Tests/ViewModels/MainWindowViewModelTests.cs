using Wslr.UI.Services;
using Wslr.UI.ViewModels;

namespace Wslr.UI.Tests.ViewModels;

public class MainWindowViewModelTests
{
    private readonly Mock<INavigationService> _navigationServiceMock;
    private readonly Mock<DistributionListViewModel> _distributionListVmMock;
    private readonly Mock<TerminalViewModel> _terminalVmMock;
    private readonly Mock<ScriptEditorViewModel> _scriptEditorVmMock;
    private readonly Mock<SettingsViewModel> _settingsVmMock;

    public MainWindowViewModelTests()
    {
        _navigationServiceMock = new Mock<INavigationService>();

        // Create minimal mocks for the child ViewModels
        _distributionListVmMock = new Mock<DistributionListViewModel>(MockBehavior.Loose, Array.Empty<object>());
        _terminalVmMock = new Mock<TerminalViewModel>(MockBehavior.Loose, Array.Empty<object>());
        _scriptEditorVmMock = new Mock<ScriptEditorViewModel>(MockBehavior.Loose, Array.Empty<object>());
        _settingsVmMock = new Mock<SettingsViewModel>(MockBehavior.Loose, Array.Empty<object>());
    }

    private MainWindowViewModel CreateViewModel()
    {
        // Since the child VMs have complex constructors, we'll test with nulls and handle in specific tests
        // For now, use a simpler approach - test the behavior we can control
        return null!; // Placeholder - actual tests use property-based assertions
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullNavigationService_ThrowsArgumentNullException()
    {
        var distributionListVm = CreateMockDistributionListViewModel();
        var terminalVm = CreateMockTerminalViewModel();
        var scriptEditorVm = CreateMockScriptEditorViewModel();
        var settingsVm = CreateMockSettingsViewModel();

        var act = () => new MainWindowViewModel(
            null!,
            distributionListVm,
            terminalVm,
            scriptEditorVm,
            settingsVm);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("navigationService");
    }

    [Fact]
    public void Constructor_WithNullDistributionListViewModel_ThrowsArgumentNullException()
    {
        var terminalVm = CreateMockTerminalViewModel();
        var scriptEditorVm = CreateMockScriptEditorViewModel();
        var settingsVm = CreateMockSettingsViewModel();

        var act = () => new MainWindowViewModel(
            _navigationServiceMock.Object,
            null!,
            terminalVm,
            scriptEditorVm,
            settingsVm);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("distributionListViewModel");
    }

    #endregion

    #region Navigation Tests

    [Fact]
    public void NavigateToDistributionsCommand_SetsCurrentViewModelAndIndex()
    {
        var distributionListVm = CreateMockDistributionListViewModel();
        var terminalVm = CreateMockTerminalViewModel();
        var scriptEditorVm = CreateMockScriptEditorViewModel();
        var settingsVm = CreateMockSettingsViewModel();

        var vm = new MainWindowViewModel(
            _navigationServiceMock.Object,
            distributionListVm,
            terminalVm,
            scriptEditorVm,
            settingsVm);

        vm.NavigateToDistributionsCommand.Execute(null);

        vm.CurrentViewModel.Should().Be(distributionListVm);
        vm.SelectedNavigationIndex.Should().Be((int)NavigationPage.Distributions);
        vm.CurrentPageTitle.Should().Be("Distributions");
    }

    [Fact]
    public void NavigateToTerminalCommand_SetsCurrentViewModelAndIndex()
    {
        var distributionListVm = CreateMockDistributionListViewModel();
        var terminalVm = CreateMockTerminalViewModel();
        var scriptEditorVm = CreateMockScriptEditorViewModel();
        var settingsVm = CreateMockSettingsViewModel();

        var vm = new MainWindowViewModel(
            _navigationServiceMock.Object,
            distributionListVm,
            terminalVm,
            scriptEditorVm,
            settingsVm);

        vm.NavigateToTerminalCommand.Execute(null);

        vm.CurrentViewModel.Should().Be(terminalVm);
        vm.SelectedNavigationIndex.Should().Be((int)NavigationPage.Terminal);
        vm.CurrentPageTitle.Should().Be("Terminal");
    }

    [Fact]
    public void NavigateToScriptsCommand_SetsCurrentViewModelAndIndex()
    {
        var distributionListVm = CreateMockDistributionListViewModel();
        var terminalVm = CreateMockTerminalViewModel();
        var scriptEditorVm = CreateMockScriptEditorViewModel();
        var settingsVm = CreateMockSettingsViewModel();

        var vm = new MainWindowViewModel(
            _navigationServiceMock.Object,
            distributionListVm,
            terminalVm,
            scriptEditorVm,
            settingsVm);

        vm.NavigateToScriptsCommand.Execute(null);

        vm.CurrentViewModel.Should().Be(scriptEditorVm);
        vm.SelectedNavigationIndex.Should().Be((int)NavigationPage.Scripts);
        vm.CurrentPageTitle.Should().Be("Scripts");
    }

    [Fact]
    public void NavigateToSettingsCommand_SetsCurrentViewModelAndIndex()
    {
        var distributionListVm = CreateMockDistributionListViewModel();
        var terminalVm = CreateMockTerminalViewModel();
        var scriptEditorVm = CreateMockScriptEditorViewModel();
        var settingsVm = CreateMockSettingsViewModel();

        var vm = new MainWindowViewModel(
            _navigationServiceMock.Object,
            distributionListVm,
            terminalVm,
            scriptEditorVm,
            settingsVm);

        vm.NavigateToSettingsCommand.Execute(null);

        vm.CurrentViewModel.Should().Be(settingsVm);
        vm.SelectedNavigationIndex.Should().Be((int)NavigationPage.Settings);
        vm.CurrentPageTitle.Should().Be("Settings");
    }

    [Fact]
    public void NavigateToInstallCommand_SetsPlaceholderViewModel()
    {
        var distributionListVm = CreateMockDistributionListViewModel();
        var terminalVm = CreateMockTerminalViewModel();
        var scriptEditorVm = CreateMockScriptEditorViewModel();
        var settingsVm = CreateMockSettingsViewModel();

        var vm = new MainWindowViewModel(
            _navigationServiceMock.Object,
            distributionListVm,
            terminalVm,
            scriptEditorVm,
            settingsVm);

        vm.NavigateToInstallCommand.Execute(null);

        vm.CurrentViewModel.Should().BeOfType<PlaceholderViewModel>();
        vm.SelectedNavigationIndex.Should().Be((int)NavigationPage.Install);
        vm.CurrentPageTitle.Should().Be("Install");
    }

    #endregion

    #region Window Actions Tests

    [Fact]
    public void MinimizeToTrayCommand_HidesMainWindow()
    {
        var distributionListVm = CreateMockDistributionListViewModel();
        var terminalVm = CreateMockTerminalViewModel();
        var scriptEditorVm = CreateMockScriptEditorViewModel();
        var settingsVm = CreateMockSettingsViewModel();

        var vm = new MainWindowViewModel(
            _navigationServiceMock.Object,
            distributionListVm,
            terminalVm,
            scriptEditorVm,
            settingsVm);

        vm.MinimizeToTrayCommand.Execute(null);

        _navigationServiceMock.Verify(s => s.HideMainWindow(), Times.Once);
    }

    [Fact]
    public void ExitCommand_ExitsApplication()
    {
        var distributionListVm = CreateMockDistributionListViewModel();
        var terminalVm = CreateMockTerminalViewModel();
        var scriptEditorVm = CreateMockScriptEditorViewModel();
        var settingsVm = CreateMockSettingsViewModel();

        var vm = new MainWindowViewModel(
            _navigationServiceMock.Object,
            distributionListVm,
            terminalVm,
            scriptEditorVm,
            settingsVm);

        vm.ExitCommand.Execute(null);

        _navigationServiceMock.Verify(s => s.ExitApplication(), Times.Once);
    }

    #endregion

    #region Initial State Tests

    [Fact]
    public void Constructor_SetsInitialViewToDistributions()
    {
        var distributionListVm = CreateMockDistributionListViewModel();
        var terminalVm = CreateMockTerminalViewModel();
        var scriptEditorVm = CreateMockScriptEditorViewModel();
        var settingsVm = CreateMockSettingsViewModel();

        var vm = new MainWindowViewModel(
            _navigationServiceMock.Object,
            distributionListVm,
            terminalVm,
            scriptEditorVm,
            settingsVm);

        vm.CurrentViewModel.Should().Be(distributionListVm);
        vm.SelectedNavigationIndex.Should().Be((int)NavigationPage.Distributions);
    }

    [Fact]
    public void Constructor_SetsDefaultTitle()
    {
        var distributionListVm = CreateMockDistributionListViewModel();
        var terminalVm = CreateMockTerminalViewModel();
        var scriptEditorVm = CreateMockScriptEditorViewModel();
        var settingsVm = CreateMockSettingsViewModel();

        var vm = new MainWindowViewModel(
            _navigationServiceMock.Object,
            distributionListVm,
            terminalVm,
            scriptEditorVm,
            settingsVm);

        vm.Title.Should().Be("WSLR - WSL Instance Manager");
    }

    [Fact]
    public void VersionString_ReturnsVersionFormat()
    {
        var distributionListVm = CreateMockDistributionListViewModel();
        var terminalVm = CreateMockTerminalViewModel();
        var scriptEditorVm = CreateMockScriptEditorViewModel();
        var settingsVm = CreateMockSettingsViewModel();

        var vm = new MainWindowViewModel(
            _navigationServiceMock.Object,
            distributionListVm,
            terminalVm,
            scriptEditorVm,
            settingsVm);

        vm.VersionString.Should().StartWith("v");
    }

    #endregion

    #region Helper Methods

    private DistributionListViewModel CreateMockDistributionListViewModel()
    {
        // Create with all required dependencies mocked
        var wslService = new Mock<Wslr.Core.Interfaces.IWslService>().Object;
        var dialogService = new Mock<IDialogService>().Object;
        var monitorService = new Mock<IDistributionMonitorService>().Object;
        var resourceMonitorService = new Mock<IResourceMonitorService>().Object;
        var distributionResourceService = new Mock<IDistributionResourceService>().Object;
        var settingsService = new Mock<ISettingsService>().Object;
        var navigationService = new Mock<INavigationService>().Object;
        var templateService = new Mock<Wslr.Core.Interfaces.IConfigurationTemplateService>().Object;
        var distroConfigService = new Mock<Wslr.Core.Interfaces.IWslDistroConfigService>().Object;
        var scriptTemplateService = new Mock<Wslr.Core.Interfaces.IScriptTemplateService>().Object;
        var scriptExecutionService = new Mock<Wslr.Core.Interfaces.IScriptExecutionService>().Object;
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<DistributionListViewModel>>().Object;

        return new DistributionListViewModel(
            wslService, dialogService, monitorService, resourceMonitorService,
            distributionResourceService, settingsService, navigationService,
            templateService, distroConfigService, scriptTemplateService,
            scriptExecutionService, logger);
    }

    private TerminalViewModel CreateMockTerminalViewModel()
    {
        var sessionService = new Mock<Wslr.Core.Interfaces.ITerminalSessionService>().Object;
        return new TerminalViewModel(sessionService);
    }

    private ScriptEditorViewModel CreateMockScriptEditorViewModel()
    {
        var scriptExecutionService = new Mock<Wslr.Core.Interfaces.IScriptExecutionService>().Object;
        var scriptTemplateService = new Mock<Wslr.Core.Interfaces.IScriptTemplateService>().Object;
        var wslService = new Mock<Wslr.Core.Interfaces.IWslService>().Object;
        var dialogService = new Mock<IDialogService>().Object;
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<ScriptEditorViewModel>>().Object;

        return new ScriptEditorViewModel(
            scriptExecutionService, scriptTemplateService, wslService, dialogService, logger);
    }

    private SettingsViewModel CreateMockSettingsViewModel()
    {
        var settingsService = new Mock<ISettingsService>().Object;
        var startupService = new Mock<IStartupService>().Object;
        var loggingService = new Mock<ILoggingService>().Object;
        var globalWslSettingsVm = CreateMockGlobalWslSettingsViewModel();
        var distroSettingsVm = CreateMockDistroSettingsViewModel();
        var templateListVm = CreateMockTemplateListViewModel();
        var profileListVm = CreateMockProfileListViewModel();

        return new SettingsViewModel(
            settingsService, startupService, loggingService,
            globalWslSettingsVm, distroSettingsVm, templateListVm, profileListVm);
    }

    private GlobalWslSettingsViewModel CreateMockGlobalWslSettingsViewModel()
    {
        var configService = new Mock<Wslr.Core.Interfaces.IWslConfigService>().Object;
        var dialogService = new Mock<IDialogService>().Object;
        return new GlobalWslSettingsViewModel(configService, dialogService);
    }

    private DistroSettingsViewModel CreateMockDistroSettingsViewModel()
    {
        var configService = new Mock<Wslr.Core.Interfaces.IWslDistroConfigService>().Object;
        var wslService = new Mock<Wslr.Core.Interfaces.IWslService>().Object;
        var dialogService = new Mock<IDialogService>().Object;
        return new DistroSettingsViewModel(configService, wslService, dialogService);
    }

    private TemplateListViewModel CreateMockTemplateListViewModel()
    {
        var templateService = new Mock<Wslr.Core.Interfaces.IConfigurationTemplateService>().Object;
        var wslService = new Mock<Wslr.Core.Interfaces.IWslService>().Object;
        var dialogService = new Mock<IDialogService>().Object;
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<TemplateListViewModel>>().Object;
        return new TemplateListViewModel(templateService, wslService, dialogService, logger);
    }

    private ProfileListViewModel CreateMockProfileListViewModel()
    {
        var profileService = new Mock<Wslr.Core.Interfaces.IConfigurationProfileService>().Object;
        var wslService = new Mock<Wslr.Core.Interfaces.IWslService>().Object;
        var dialogService = new Mock<IDialogService>().Object;
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<ProfileListViewModel>>().Object;
        return new ProfileListViewModel(profileService, wslService, dialogService, logger);
    }

    #endregion
}
