using Wslr.Core.Interfaces;
using Wslr.Core.Models;
using Wslr.UI.Services;
using Wslr.UI.ViewModels;
using Microsoft.Extensions.Logging;
using MockFactoryHelper = Wslr.UI.Tests.Helpers.MockFactory;

namespace Wslr.UI.Tests.ViewModels;

public class TemplateListViewModelTests
{
    private readonly Mock<IConfigurationTemplateService> _templateServiceMock;
    private readonly Mock<IWslService> _wslServiceMock;
    private readonly Mock<IDialogService> _dialogServiceMock;
    private readonly Mock<ILogger<TemplateListViewModel>> _loggerMock;

    public TemplateListViewModelTests()
    {
        _templateServiceMock = new Mock<IConfigurationTemplateService>();
        _wslServiceMock = new Mock<IWslService>();
        _dialogServiceMock = new Mock<IDialogService>();
        _loggerMock = new Mock<ILogger<TemplateListViewModel>>();
    }

    private TemplateListViewModel CreateViewModel()
    {
        return new TemplateListViewModel(
            _templateServiceMock.Object,
            _wslServiceMock.Object,
            _dialogServiceMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullTemplateService_ThrowsArgumentNullException()
    {
        var act = () => new TemplateListViewModel(
            null!,
            _wslServiceMock.Object,
            _dialogServiceMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("templateService");
    }

    [Fact]
    public void Constructor_InitializesWithEmptyState()
    {
        var vm = CreateViewModel();

        vm.Templates.Should().BeEmpty();
        vm.Distributions.Should().BeEmpty();
        vm.SelectedTemplate.Should().BeNull();
        vm.IsLoading.Should().BeFalse();
        vm.IsEditing.Should().BeFalse();
    }

    #endregion

    #region LoadAsync Tests

    [Fact]
    public async Task LoadAsync_LoadsTemplatesAndDistributions()
    {
        var templates = new List<ConfigurationTemplate>
        {
            new() { Id = "1", Name = "Template 1" },
            new() { Id = "2", Name = "Template 2" }
        };
        var distributions = new List<WslDistribution>
        {
            MockFactoryHelper.CreateDistribution("Ubuntu"),
            MockFactoryHelper.CreateDistribution("Debian")
        };

        _templateServiceMock.Setup(s => s.GetAllTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);
        _wslServiceMock.Setup(s => s.GetDistributionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(distributions);

        var vm = CreateViewModel();
        await vm.LoadAsync();

        vm.Templates.Should().HaveCount(2);
        vm.Distributions.Should().HaveCount(2);
        vm.SelectedDistribution.Should().Be("Debian"); // Sorted alphabetically
    }

    #endregion

    #region NewTemplate Tests

    [Fact]
    public void NewTemplate_EntersEditingMode()
    {
        var vm = CreateViewModel();

        vm.NewTemplateCommand.Execute(null);

        vm.IsEditing.Should().BeTrue();
        vm.EditName.Should().Be("New Template");
        vm.EditDescription.Should().BeEmpty();
        vm.SelectedTemplate.Should().BeNull();
    }

    #endregion

    #region SaveTemplateAsync Tests

    [Fact]
    public async Task SaveTemplateAsync_WithEmptyName_SetsError()
    {
        var vm = CreateViewModel();
        vm.EditName = "";

        await vm.SaveTemplateCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Contain("required");
    }

    [Fact]
    public async Task SaveTemplateAsync_CreatesNewTemplate()
    {
        var createdTemplate = new ConfigurationTemplate { Id = "new", Name = "New Template" };
        _templateServiceMock.Setup(s => s.CreateTemplateAsync(It.IsAny<ConfigurationTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdTemplate);

        var vm = CreateViewModel();
        vm.NewTemplateCommand.Execute(null);
        vm.EditName = "New Template";

        await vm.SaveTemplateCommand.ExecuteAsync(null);

        vm.IsEditing.Should().BeFalse();
        vm.SuccessMessage.Should().Contain("saved");
        vm.Templates.Should().ContainSingle(t => t.Name == "New Template");
    }

    #endregion

    #region EditTemplate Tests

    [Fact]
    public void EditTemplate_WithBuiltInTemplate_SetsError()
    {
        var vm = CreateViewModel();
        vm.SelectedTemplate = new TemplateItemViewModel { Id = "builtin", IsBuiltIn = true };

        vm.EditTemplateCommand.Execute(null);

        vm.ErrorMessage.Should().Contain("Built-in");
        vm.IsEditing.Should().BeFalse();
    }

    [Fact]
    public void EditTemplate_EntersEditingMode()
    {
        var vm = CreateViewModel();
        vm.SelectedTemplate = new TemplateItemViewModel
        {
            Id = "user",
            Name = "My Template",
            Description = "Description",
            IsBuiltIn = false
        };

        vm.EditTemplateCommand.Execute(null);

        vm.IsEditing.Should().BeTrue();
        vm.EditName.Should().Be("My Template");
        vm.EditDescription.Should().Be("Description");
    }

    #endregion

    #region DeleteTemplateAsync Tests

    [Fact]
    public async Task DeleteTemplateAsync_WithBuiltInTemplate_SetsError()
    {
        var vm = CreateViewModel();
        vm.SelectedTemplate = new TemplateItemViewModel { Id = "builtin", IsBuiltIn = true };

        await vm.DeleteTemplateCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Contain("Built-in");
    }

    [Fact]
    public async Task DeleteTemplateAsync_WhenConfirmed_DeletesTemplate()
    {
        _dialogServiceMock.Setup(s => s.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var template = new TemplateItemViewModel { Id = "user", Name = "My Template", IsBuiltIn = false };
        var vm = CreateViewModel();
        vm.Templates.Add(template);
        vm.SelectedTemplate = template;

        await vm.DeleteTemplateCommand.ExecuteAsync(null);

        _templateServiceMock.Verify(s => s.DeleteTemplateAsync("user", It.IsAny<CancellationToken>()), Times.Once);
        vm.Templates.Should().BeEmpty();
        vm.SelectedTemplate.Should().BeNull();
    }

    #endregion

    #region DuplicateTemplateAsync Tests

    [Fact]
    public async Task DuplicateTemplateAsync_CreatesDuplicate()
    {
        var duplicate = new ConfigurationTemplate { Id = "dup", Name = "My Template (Copy)" };
        _templateServiceMock.Setup(s => s.DuplicateTemplateAsync("original", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(duplicate);

        var vm = CreateViewModel();
        vm.SelectedTemplate = new TemplateItemViewModel { Id = "original", Name = "My Template" };

        await vm.DuplicateTemplateCommand.ExecuteAsync(null);

        vm.Templates.Should().ContainSingle(t => t.Name == "My Template (Copy)");
        vm.SuccessMessage.Should().Contain("duplicated");
    }

    #endregion

    #region ApplyTemplateAsync Tests

    [Fact]
    public async Task ApplyTemplateAsync_WithNoTemplate_SetsError()
    {
        var vm = CreateViewModel();
        vm.SelectedTemplate = null;
        vm.SelectedDistribution = "Ubuntu";

        await vm.ApplyTemplateCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Contain("template");
    }

    [Fact]
    public async Task ApplyTemplateAsync_WithNoDistribution_SetsError()
    {
        var vm = CreateViewModel();
        vm.SelectedTemplate = new TemplateItemViewModel { Id = "1" };
        vm.SelectedDistribution = null;

        await vm.ApplyTemplateCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Contain("distribution");
    }

    [Fact]
    public async Task ApplyTemplateAsync_WhenConfirmed_AppliesTemplate()
    {
        _dialogServiceMock.Setup(s => s.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _templateServiceMock.Setup(s => s.ApplyTemplateAsync("1", "Ubuntu", It.IsAny<TemplateApplyOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TemplateApplyResult
            {
                Success = true,
                GlobalSettingsApplied = true,
                DistroSettingsApplied = true,
                RestartRequired = true
            });

        var vm = CreateViewModel();
        vm.SelectedTemplate = new TemplateItemViewModel { Id = "1", Name = "Development" };
        vm.SelectedDistribution = "Ubuntu";
        vm.ApplyGlobalSettings = true;
        vm.ApplyDistroSettings = true;

        await vm.ApplyTemplateCommand.ExecuteAsync(null);

        _templateServiceMock.Verify(s => s.ApplyTemplateAsync("1", "Ubuntu", It.IsAny<TemplateApplyOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        vm.SuccessMessage.Should().Contain("Applied");
    }

    #endregion

    #region CreateFromDistributionAsync Tests

    [Fact]
    public async Task CreateFromDistributionAsync_WithNoDistribution_SetsError()
    {
        var vm = CreateViewModel();
        vm.SelectedDistribution = null;

        await vm.CreateFromDistributionCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Contain("distribution");
    }

    [Fact]
    public async Task CreateFromDistributionAsync_WhenConfirmed_CreatesTemplate()
    {
        _dialogServiceMock.Setup(s => s.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _templateServiceMock.Setup(s => s.CreateTemplateFromDistributionAsync(
            It.IsAny<string>(), It.IsAny<string>(), "Ubuntu", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConfigurationTemplate { Id = "new", Name = "From Ubuntu" });

        var vm = CreateViewModel();
        vm.SelectedDistribution = "Ubuntu";

        await vm.CreateFromDistributionCommand.ExecuteAsync(null);

        vm.Templates.Should().ContainSingle(t => t.Name == "From Ubuntu");
        vm.SuccessMessage.Should().Contain("created");
    }

    #endregion

    #region Import/Export Tests

    [Fact]
    public async Task ExportTemplateAsync_ExportsToFile()
    {
        _dialogServiceMock.Setup(s => s.ShowSaveFileDialogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(@"C:\test\template.json");

        var vm = CreateViewModel();
        vm.SelectedTemplate = new TemplateItemViewModel { Id = "1", Name = "My Template" };

        await vm.ExportTemplateCommand.ExecuteAsync(null);

        _templateServiceMock.Verify(s => s.ExportTemplateAsync("1", @"C:\test\template.json", It.IsAny<CancellationToken>()), Times.Once);
        vm.SuccessMessage.Should().Contain("exported");
    }

    [Fact]
    public async Task ImportTemplateAsync_ImportsFromFile()
    {
        _dialogServiceMock.Setup(s => s.ShowOpenFileDialogAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(@"C:\test\template.json");
        _templateServiceMock.Setup(s => s.ImportTemplateAsync(@"C:\test\template.json", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConfigurationTemplate { Id = "imported", Name = "Imported Template" });

        var vm = CreateViewModel();

        await vm.ImportTemplateCommand.ExecuteAsync(null);

        vm.Templates.Should().ContainSingle(t => t.Name == "Imported Template");
        vm.SuccessMessage.Should().Contain("imported");
    }

    #endregion

    #region CancelEdit Tests

    [Fact]
    public void CancelEdit_ExitsEditingMode()
    {
        var vm = CreateViewModel();
        vm.IsEditing = true;
        vm.EditName = "Test";
        vm.EditDescription = "Desc";

        vm.CancelEditCommand.Execute(null);

        vm.IsEditing.Should().BeFalse();
        vm.EditName.Should().BeEmpty();
        vm.EditDescription.Should().BeEmpty();
    }

    #endregion

    #region Apply Options Tests

    [Fact]
    public void ApplyOptions_DefaultValues()
    {
        var vm = CreateViewModel();

        vm.ApplyGlobalSettings.Should().BeTrue();
        vm.ApplyDistroSettings.Should().BeTrue();
        vm.UseMergeMode.Should().BeTrue();
    }

    #endregion
}
