using Wslr.Core.Interfaces;
using Wslr.Core.Models;
using Wslr.UI.Services;
using Wslr.UI.ViewModels;
using Microsoft.Extensions.Logging;
using MockFactoryHelper = Wslr.UI.Tests.Helpers.MockFactory;

namespace Wslr.UI.Tests.ViewModels;

public class ScriptEditorViewModelTests
{
    private readonly Mock<IScriptExecutionService> _scriptExecutionServiceMock;
    private readonly Mock<IScriptTemplateService> _scriptTemplateServiceMock;
    private readonly Mock<IWslService> _wslServiceMock;
    private readonly Mock<IDialogService> _dialogServiceMock;
    private readonly Mock<ILogger<ScriptEditorViewModel>> _loggerMock;

    public ScriptEditorViewModelTests()
    {
        _scriptExecutionServiceMock = new Mock<IScriptExecutionService>();
        _scriptTemplateServiceMock = new Mock<IScriptTemplateService>();
        _wslServiceMock = new Mock<IWslService>();
        _dialogServiceMock = new Mock<IDialogService>();
        _loggerMock = new Mock<ILogger<ScriptEditorViewModel>>();
    }

    private ScriptEditorViewModel CreateViewModel()
    {
        return new ScriptEditorViewModel(
            _scriptExecutionServiceMock.Object,
            _scriptTemplateServiceMock.Object,
            _wslServiceMock.Object,
            _dialogServiceMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullScriptExecutionService_ThrowsArgumentNullException()
    {
        var act = () => new ScriptEditorViewModel(
            null!,
            _scriptTemplateServiceMock.Object,
            _wslServiceMock.Object,
            _dialogServiceMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("scriptExecutionService");
    }

    [Fact]
    public void Constructor_InitializesWithDefaultState()
    {
        var vm = CreateViewModel();

        vm.ScriptName.Should().Be("Untitled Script");
        vm.ScriptContent.Should().Contain("#!/bin/bash");
        vm.IsRunning.Should().BeFalse();
        vm.IsModified.Should().BeFalse();
        vm.AvailableDistributions.Should().BeEmpty();
        vm.AvailableTemplates.Should().BeEmpty();
    }

    #endregion

    #region LoadAsync Tests

    [Fact]
    public async Task LoadAsync_LoadsDistributionsAndTemplates()
    {
        _wslServiceMock.Setup(s => s.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>
            {
                MockFactoryHelper.CreateDistribution("Ubuntu", DistributionState.Running),
                MockFactoryHelper.CreateDistribution("Debian", DistributionState.Stopped)
            });
        _scriptTemplateServiceMock.Setup(s => s.GetAllTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScriptTemplate>
            {
                new() { Id = "1", Name = "Template 1", ScriptContent = "echo 1" },
                new() { Id = "2", Name = "Template 2", ScriptContent = "echo 2" }
            });

        var vm = CreateViewModel();
        await vm.LoadAsync();

        vm.AvailableDistributions.Should().HaveCount(2);
        vm.AvailableTemplates.Should().HaveCount(2);
        vm.SelectedDistribution.Should().Be("Ubuntu"); // First one selected
    }

    #endregion

    #region RunScriptAsync Tests

    [Fact]
    public async Task RunScriptAsync_WithNoDistribution_SetsErrorMessage()
    {
        var vm = CreateViewModel();
        vm.SelectedDistribution = null;

        await vm.RunScriptCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Contain("select a distribution");
    }

    [Fact]
    public async Task RunScriptAsync_WithEmptyContent_SetsErrorMessage()
    {
        var vm = CreateViewModel();
        vm.SelectedDistribution = "Ubuntu";
        vm.ScriptContent = "";

        await vm.RunScriptCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Contain("empty");
    }

    [Fact]
    public async Task RunScriptAsync_ExecutesScriptSuccessfully()
    {
        var result = new ScriptExecutionResult
        {
            ExitCode = 0,
            StandardOutput = "test",
            StandardError = "",
            Duration = TimeSpan.FromSeconds(1.5)
        };
        _scriptExecutionServiceMock.Setup(s => s.ExecuteScriptAsync(
            "Ubuntu",
            It.IsAny<string>(),
            It.IsAny<IReadOnlyDictionary<string, string>?>(),
            It.IsAny<IProgress<string>?>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var vm = CreateViewModel();
        vm.SelectedDistribution = "Ubuntu";
        vm.ScriptContent = "echo test";

        await vm.RunScriptCommand.ExecuteAsync(null);

        vm.LastExitCode.Should().Be(0);
        vm.SuccessMessage.Should().Contain("successfully");
        vm.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task RunScriptAsync_SetsErrorMessage_OnFailure()
    {
        var result = new ScriptExecutionResult
        {
            ExitCode = 1,
            StandardOutput = "",
            StandardError = "Command not found",
            Duration = TimeSpan.FromSeconds(0.5)
        };
        _scriptExecutionServiceMock.Setup(s => s.ExecuteScriptAsync(
            "Ubuntu",
            It.IsAny<string>(),
            It.IsAny<IReadOnlyDictionary<string, string>?>(),
            It.IsAny<IProgress<string>?>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var vm = CreateViewModel();
        vm.SelectedDistribution = "Ubuntu";
        vm.ScriptContent = "invalid_command";

        await vm.RunScriptCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Contain("exit code 1");
        vm.OutputContent.Should().Contain("Command not found");
    }

    #endregion

    #region ValidateScriptAsync Tests

    [Fact]
    public async Task ValidateScriptAsync_WhenValid_ShowsSuccess()
    {
        _scriptExecutionServiceMock.Setup(s => s.ValidateScriptAsync(
            "Ubuntu",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(ScriptValidationResult.Success);

        var vm = CreateViewModel();
        vm.SelectedDistribution = "Ubuntu";
        vm.ScriptContent = "echo test";

        await vm.ValidateScriptCommand.ExecuteAsync(null);

        vm.IsValidationSuccess.Should().BeTrue();
        vm.ValidationMessage.Should().Contain("valid");
        vm.SuccessMessage.Should().Contain("passed");
    }

    [Fact]
    public async Task ValidateScriptAsync_WhenInvalid_ShowsError()
    {
        _scriptExecutionServiceMock.Setup(s => s.ValidateScriptAsync(
            "Ubuntu",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(ScriptValidationResult.Failure("syntax error", 5));

        var vm = CreateViewModel();
        vm.SelectedDistribution = "Ubuntu";
        vm.ScriptContent = "echo test";

        await vm.ValidateScriptCommand.ExecuteAsync(null);

        vm.IsValidationSuccess.Should().BeFalse();
        vm.ValidationMessage.Should().Contain("syntax error");
        vm.ValidationMessage.Should().Contain("line 5");
    }

    #endregion

    #region SaveScriptAsync Tests

    [Fact]
    public async Task SaveScriptAsync_WithEmptyName_SetsErrorMessage()
    {
        var vm = CreateViewModel();
        vm.ScriptName = "";

        await vm.SaveScriptCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Contain("name");
    }

    [Fact]
    public async Task SaveScriptAsync_CreatesNewTemplate()
    {
        var createdTemplate = new ScriptTemplate { Id = "new-id", Name = "My Script", ScriptContent = "echo test" };
        _scriptTemplateServiceMock.Setup(s => s.CreateTemplateAsync(It.IsAny<ScriptTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdTemplate);
        _wslServiceMock.Setup(s => s.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>());
        _scriptTemplateServiceMock.Setup(s => s.GetAllTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScriptTemplate>());

        var vm = CreateViewModel();
        vm.ScriptName = "My Script";
        vm.ScriptContent = "echo test";
        vm.CurrentTemplate = null;

        await vm.SaveScriptCommand.ExecuteAsync(null);

        vm.SuccessMessage.Should().Contain("saved");
        vm.IsModified.Should().BeFalse();
        vm.CurrentTemplate.Should().Be(createdTemplate);
    }

    #endregion

    #region NewScript Tests

    [Fact]
    public void NewScript_ResetsEditor()
    {
        var vm = CreateViewModel();
        vm.ScriptName = "Modified";
        vm.ScriptContent = "some content";
        vm.OutputContent = "output";
        vm.CurrentTemplate = new ScriptTemplate { Id = "test", Name = "Test", ScriptContent = "echo test" };
        vm.IsModified = true;

        vm.NewScriptCommand.Execute(null);

        vm.ScriptName.Should().Be("Untitled Script");
        vm.ScriptContent.Should().Contain("#!/bin/bash");
        vm.OutputContent.Should().BeEmpty();
        vm.CurrentTemplate.Should().BeNull();
        vm.IsModified.Should().BeFalse();
    }

    #endregion

    #region ClearOutput Tests

    [Fact]
    public void ClearOutput_ClearsOutputAndMessages()
    {
        var vm = CreateViewModel();
        vm.OutputContent = "some output";
        vm.ErrorMessage = "some error";
        vm.SuccessMessage = "some success";

        vm.ClearOutputCommand.Execute(null);

        vm.OutputContent.Should().BeEmpty();
        vm.ErrorMessage.Should().BeNull();
        vm.SuccessMessage.Should().BeNull();
    }

    #endregion

    #region LoadTemplate Tests

    [Fact]
    public void LoadTemplate_LoadsTemplateIntoEditor()
    {
        var template = new ScriptTemplate
        {
            Id = "test",
            Name = "Test Template",
            Description = "Test description",
            ScriptContent = "echo hello"
        };

        var vm = CreateViewModel();
        vm.LoadTemplateCommand.Execute(template);

        vm.ScriptName.Should().Be("Test Template");
        vm.ScriptDescription.Should().Be("Test description");
        vm.ScriptContent.Should().Be("echo hello");
        vm.CurrentTemplate.Should().Be(template);
        vm.IsModified.Should().BeFalse();
    }

    #endregion

    #region DeleteTemplateAsync Tests

    [Fact]
    public async Task DeleteTemplateAsync_WhenBuiltIn_ShowsError()
    {
        var vm = CreateViewModel();
        vm.CurrentTemplate = new ScriptTemplate { Id = "builtin", Name = "Built-in", ScriptContent = "echo test", IsBuiltIn = true };

        await vm.DeleteTemplateCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Contain("built-in");
    }

    [Fact]
    public async Task DeleteTemplateAsync_WhenConfirmed_DeletesTemplate()
    {
        _dialogServiceMock.Setup(s => s.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _wslServiceMock.Setup(s => s.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WslDistribution>());
        _scriptTemplateServiceMock.Setup(s => s.GetAllTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScriptTemplate>());

        var vm = CreateViewModel();
        vm.CurrentTemplate = new ScriptTemplate { Id = "user-template", Name = "My Template", ScriptContent = "echo test", IsBuiltIn = false };

        await vm.DeleteTemplateCommand.ExecuteAsync(null);

        _scriptTemplateServiceMock.Verify(s => s.DeleteTemplateAsync("user-template", It.IsAny<CancellationToken>()), Times.Once);
        vm.SuccessMessage.Should().Contain("Deleted");
    }

    #endregion

    #region Property Change Tests

    [Fact]
    public void ScriptContent_WhenChanged_SetsIsModified()
    {
        var vm = CreateViewModel();
        vm.IsModified = false;

        vm.ScriptContent = "new content";

        vm.IsModified.Should().BeTrue();
        vm.ValidationMessage.Should().BeNull(); // Validation cleared
    }

    [Fact]
    public void ScriptName_WhenChanged_SetsIsModified()
    {
        var vm = CreateViewModel();
        vm.IsModified = false;

        vm.ScriptName = "New Name";

        vm.IsModified.Should().BeTrue();
    }

    #endregion

    #region CanExecute Tests

    [Fact]
    public void RunScriptCommand_CannotExecute_WhenAlreadyRunning()
    {
        var vm = CreateViewModel();
        vm.SelectedDistribution = "Ubuntu";

        // Use reflection to set IsRunning since it's set internally
        typeof(ScriptEditorViewModel)
            .GetProperty("IsRunning")!
            .SetValue(vm, true);

        vm.RunScriptCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void RunScriptCommand_CannotExecute_WhenNoDistributionSelected()
    {
        var vm = CreateViewModel();
        vm.SelectedDistribution = null;

        vm.RunScriptCommand.CanExecute(null).Should().BeFalse();
    }

    #endregion
}
