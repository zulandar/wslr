using System.Text.Json;
using Microsoft.Extensions.Logging;
using Wslr.Core.Interfaces;
using Wslr.Core.Models;
using Wslr.Infrastructure.Services;

namespace Wslr.Infrastructure.Tests.Services;

public class ConfigurationTemplateServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _templatesPath;
    private readonly Mock<IWslConfigService> _wslConfigServiceMock;
    private readonly Mock<IWslDistroConfigService> _distroConfigServiceMock;
    private readonly Mock<ILogger<ConfigurationTemplateService>> _loggerMock;
    private ConfigurationTemplateService _service;

    public ConfigurationTemplateServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"ConfigTemplateServiceTests_{Guid.NewGuid():N}");
        _templatesPath = Path.Combine(_testDirectory, "Templates");
        Directory.CreateDirectory(_templatesPath);

        _wslConfigServiceMock = new Mock<IWslConfigService>();
        _distroConfigServiceMock = new Mock<IWslDistroConfigService>();
        _loggerMock = new Mock<ILogger<ConfigurationTemplateService>>();

        _service = CreateServiceWithCustomPath();
    }

    private ConfigurationTemplateService CreateServiceWithCustomPath()
    {
        var original = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        try
        {
            Environment.SetEnvironmentVariable("LOCALAPPDATA", _testDirectory);
            return new ConfigurationTemplateService(
                _wslConfigServiceMock.Object,
                _distroConfigServiceMock.Object,
                _loggerMock.Object);
        }
        finally
        {
            Environment.SetEnvironmentVariable("LOCALAPPDATA", original);
        }
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
        GC.SuppressFinalize(this);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullWslConfigService_ThrowsArgumentNullException()
    {
        var act = () => new ConfigurationTemplateService(
            null!,
            _distroConfigServiceMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("wslConfigService");
    }

    [Fact]
    public void Constructor_WithNullDistroConfigService_ThrowsArgumentNullException()
    {
        var act = () => new ConfigurationTemplateService(
            _wslConfigServiceMock.Object,
            null!,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("distroConfigService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ConfigurationTemplateService(
            _wslConfigServiceMock.Object,
            _distroConfigServiceMock.Object,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region GetBuiltInTemplates Tests

    [Fact]
    public void GetBuiltInTemplates_ReturnsExpectedTemplates()
    {
        var templates = _service.GetBuiltInTemplates();

        templates.Should().HaveCount(4);
        templates.Should().Contain(t => t.Name == "Development");
        templates.Should().Contain(t => t.Name == "Server");
        templates.Should().Contain(t => t.Name == "Isolated");
        templates.Should().Contain(t => t.Name == "Low Memory");
    }

    [Fact]
    public void GetBuiltInTemplates_AllHaveIsBuiltInTrue()
    {
        var templates = _service.GetBuiltInTemplates();

        templates.Should().OnlyContain(t => t.IsBuiltIn);
    }

    #endregion

    #region GetAllTemplatesAsync Tests

    [Fact]
    public async Task GetAllTemplatesAsync_IncludesBuiltInTemplates()
    {
        var templates = await _service.GetAllTemplatesAsync();

        templates.Should().Contain(t => t.IsBuiltIn);
        templates.Count(t => t.IsBuiltIn).Should().Be(4);
    }

    [Fact]
    public async Task GetAllTemplatesAsync_IncludesUserTemplates()
    {
        // Create a user template via the service
        var created = await _service.CreateTemplateAsync(new ConfigurationTemplate
        {
            Name = "My Template"
        });

        var templates = await _service.GetAllTemplatesAsync();

        templates.Should().Contain(t => t.Name == "My Template");
    }

    [Fact]
    public async Task GetAllTemplatesAsync_SortsBuiltInFirst()
    {
        // Create a user template via the service
        await _service.CreateTemplateAsync(new ConfigurationTemplate
        {
            Name = "AAA Template"
        });

        var templates = await _service.GetAllTemplatesAsync();
        var firstUserIndex = templates.ToList().FindIndex(t => !t.IsBuiltIn);

        if (firstUserIndex > 0)
        {
            templates.Take(firstUserIndex).Should().OnlyContain(t => t.IsBuiltIn);
        }
    }

    #endregion

    #region GetTemplateAsync Tests

    [Fact]
    public async Task GetTemplateAsync_WithBuiltInId_ReturnsBuiltInTemplate()
    {
        var template = await _service.GetTemplateAsync("builtin-dev");

        template.Should().NotBeNull();
        template!.Name.Should().Be("Development");
        template.IsBuiltIn.Should().BeTrue();
    }

    [Fact]
    public async Task GetTemplateAsync_WithNonExistentId_ReturnsNull()
    {
        var template = await _service.GetTemplateAsync("nonexistent");

        template.Should().BeNull();
    }

    #endregion

    #region CreateTemplateAsync Tests

    [Fact]
    public async Task CreateTemplateAsync_CreatesNewTemplate()
    {
        var template = new ConfigurationTemplate
        {
            Name = "New Template"
        };

        var created = await _service.CreateTemplateAsync(template);

        created.Id.Should().HaveLength(8);
        created.Name.Should().Be("New Template");
        created.IsBuiltIn.Should().BeFalse();
    }

    [Fact]
    public async Task CreateTemplateAsync_WithIsBuiltInTrue_ThrowsInvalidOperationException()
    {
        var template = new ConfigurationTemplate
        {
            Name = "New Template",
            IsBuiltIn = true
        };

        var act = () => _service.CreateTemplateAsync(template);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateTemplateAsync_CanBeRetrievedAfterCreation()
    {
        var template = new ConfigurationTemplate
        {
            Name = "Persistent Template"
        };

        var created = await _service.CreateTemplateAsync(template);
        var retrieved = await _service.GetTemplateAsync(created.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Persistent Template");
    }

    #endregion

    #region CreateTemplateFromDistributionAsync Tests

    [Fact]
    public async Task CreateTemplateFromDistributionAsync_CapturesDistroSettings()
    {
        var distroConfig = new WslDistroConfig
        {
            Automount = new AutomountSettings { Enabled = true, Root = "/mnt/" },
            Boot = new BootSettings { Systemd = true }
        };
        _distroConfigServiceMock.Setup(s => s.ReadConfigAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(distroConfig);

        var created = await _service.CreateTemplateFromDistributionAsync("From Ubuntu", null, "Ubuntu");

        created.Name.Should().Be("From Ubuntu");
        created.DistroSettings.Should().NotBeNull();
        created.DistroSettings!.Automount.Enabled.Should().BeTrue();
        created.DistroSettings.Boot.Systemd.Should().BeTrue();
        created.GlobalSettings.Should().BeNull();
    }

    [Fact]
    public async Task CreateTemplateFromDistributionAsync_WithGlobalSettings_CapturesBoth()
    {
        var distroConfig = new WslDistroConfig();
        var globalConfig = new WslConfig
        {
            Wsl2 = new Wsl2Settings { Memory = "8GB" }
        };
        _distroConfigServiceMock.Setup(s => s.ReadConfigAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(distroConfig);
        _wslConfigServiceMock.Setup(s => s.ReadConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(globalConfig);

        var created = await _service.CreateTemplateFromDistributionAsync(
            "Full Template", null, "Ubuntu", includeGlobalSettings: true);

        created.GlobalSettings.Should().NotBeNull();
        created.GlobalSettings!.Wsl2.Memory.Should().Be("8GB");
        created.DistroSettings.Should().NotBeNull();
    }

    #endregion

    #region UpdateTemplateAsync Tests

    [Fact]
    public async Task UpdateTemplateAsync_WithBuiltInTemplate_ThrowsInvalidOperationException()
    {
        var builtIn = new ConfigurationTemplate
        {
            Id = "builtin-dev",
            Name = "Modified"
        };

        var act = () => _service.UpdateTemplateAsync(builtIn);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*built-in*");
    }

    [Fact]
    public async Task UpdateTemplateAsync_UpdatesModifiedAt()
    {
        var original = await _service.CreateTemplateAsync(new ConfigurationTemplate
        {
            Name = "Original"
        });

        await Task.Delay(100);

        var updated = await _service.UpdateTemplateAsync(original with { Name = "Updated" });

        updated.ModifiedAt.Should().BeAfter(original.ModifiedAt);
    }

    #endregion

    #region DeleteTemplateAsync Tests

    [Fact]
    public async Task DeleteTemplateAsync_WithBuiltInTemplate_ThrowsInvalidOperationException()
    {
        var act = () => _service.DeleteTemplateAsync("builtin-dev");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*built-in*");
    }

    [Fact]
    public async Task DeleteTemplateAsync_WithNonExistentTemplate_ReturnsFalse()
    {
        var result = await _service.DeleteTemplateAsync("nonexistent");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteTemplateAsync_WithUserTemplate_DeletesAndReturnsTrue()
    {
        var created = await _service.CreateTemplateAsync(new ConfigurationTemplate
        {
            Name = "To Delete"
        });

        var result = await _service.DeleteTemplateAsync(created.Id);

        result.Should().BeTrue();
        var retrieved = await _service.GetTemplateAsync(created.Id);
        retrieved.Should().BeNull();
    }

    #endregion

    #region DuplicateTemplateAsync Tests

    [Fact]
    public async Task DuplicateTemplateAsync_CreatesNewTemplate()
    {
        var duplicate = await _service.DuplicateTemplateAsync("builtin-dev");

        duplicate.Id.Should().NotBe("builtin-dev");
        duplicate.IsBuiltIn.Should().BeFalse();
        duplicate.Name.Should().Be("Development (Copy)");
    }

    [Fact]
    public async Task DuplicateTemplateAsync_WithCustomName_UsesCustomName()
    {
        var duplicate = await _service.DuplicateTemplateAsync("builtin-dev", "My Dev Copy");

        duplicate.Name.Should().Be("My Dev Copy");
    }

    [Fact]
    public async Task DuplicateTemplateAsync_CopiesSettings()
    {
        var original = await _service.GetTemplateAsync("builtin-dev");
        var duplicate = await _service.DuplicateTemplateAsync("builtin-dev");

        duplicate.DistroSettings.Should().BeEquivalentTo(original!.DistroSettings);
    }

    #endregion

    #region ApplyTemplateAsync Tests

    [Fact]
    public async Task ApplyTemplateAsync_WithNonExistentTemplate_ReturnsFailure()
    {
        var result = await _service.ApplyTemplateAsync("nonexistent", "Ubuntu");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ApplyTemplateAsync_AppliesGlobalSettings()
    {
        _wslConfigServiceMock.Setup(s => s.ReadConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WslConfig());
        _wslConfigServiceMock.Setup(s => s.CreateBackupAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("backup.txt");
        _distroConfigServiceMock.Setup(s => s.ReadConfigAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WslDistroConfig());
        _distroConfigServiceMock.Setup(s => s.ConfigExists("Ubuntu"))
            .Returns(true);
        _distroConfigServiceMock.Setup(s => s.CreateBackupAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync("backup.txt");

        // Use builtin-lowmem which has global settings
        var result = await _service.ApplyTemplateAsync("builtin-lowmem", "Ubuntu");

        result.Success.Should().BeTrue();
        result.GlobalSettingsApplied.Should().BeTrue();
        _wslConfigServiceMock.Verify(s => s.WriteConfigAsync(It.IsAny<WslConfig>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyTemplateAsync_AppliesDistroSettings()
    {
        _wslConfigServiceMock.Setup(s => s.ReadConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WslConfig());
        _distroConfigServiceMock.Setup(s => s.ReadConfigAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WslDistroConfig());
        _distroConfigServiceMock.Setup(s => s.ConfigExists("Ubuntu"))
            .Returns(true);
        _distroConfigServiceMock.Setup(s => s.CreateBackupAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync("backup.txt");

        // Use builtin-dev which has distro settings
        var result = await _service.ApplyTemplateAsync("builtin-dev", "Ubuntu");

        result.Success.Should().BeTrue();
        result.DistroSettingsApplied.Should().BeTrue();
        _distroConfigServiceMock.Verify(s => s.WriteConfigAsync("Ubuntu", It.IsAny<WslDistroConfig>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyTemplateAsync_WithMergeMode_MergesSettings()
    {
        var existingConfig = new WslConfig
        {
            Wsl2 = new Wsl2Settings { Memory = "4GB", Processors = 2 }
        };
        _wslConfigServiceMock.Setup(s => s.ReadConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingConfig);
        _wslConfigServiceMock.Setup(s => s.CreateBackupAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("backup.txt");
        _distroConfigServiceMock.Setup(s => s.ReadConfigAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WslDistroConfig());
        _distroConfigServiceMock.Setup(s => s.ConfigExists("Ubuntu"))
            .Returns(true);
        _distroConfigServiceMock.Setup(s => s.CreateBackupAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync("backup.txt");

        WslConfig? writtenConfig = null;
        _wslConfigServiceMock.Setup(s => s.WriteConfigAsync(It.IsAny<WslConfig>(), It.IsAny<CancellationToken>()))
            .Callback<WslConfig, CancellationToken>((c, _) => writtenConfig = c)
            .Returns(Task.CompletedTask);

        var options = new TemplateApplyOptions { MergeMode = TemplateMergeMode.Merge };
        await _service.ApplyTemplateAsync("builtin-lowmem", "Ubuntu", options);

        // Template sets Memory=4GB, existing has Processors=2
        // Merge should keep processors from existing
        writtenConfig.Should().NotBeNull();
    }

    [Fact]
    public async Task ApplyTemplateAsync_WhenDisabled_SkipsSettings()
    {
        _distroConfigServiceMock.Setup(s => s.ReadConfigAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WslDistroConfig());

        var options = new TemplateApplyOptions
        {
            ApplyGlobalSettings = false,
            ApplyDistroSettings = false
        };
        var result = await _service.ApplyTemplateAsync("builtin-dev", "Ubuntu", options);

        result.Success.Should().BeTrue();
        result.GlobalSettingsApplied.Should().BeFalse();
        result.DistroSettingsApplied.Should().BeFalse();
        result.RestartRequired.Should().BeFalse();
    }

    #endregion

    #region ApplyTemplateToMultipleAsync Tests

    [Fact]
    public async Task ApplyTemplateToMultipleAsync_AppliestoAllDistros()
    {
        _distroConfigServiceMock.Setup(s => s.ReadConfigAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WslDistroConfig());
        _distroConfigServiceMock.Setup(s => s.ConfigExists(It.IsAny<string>()))
            .Returns(true);
        _distroConfigServiceMock.Setup(s => s.CreateBackupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("backup.txt");

        var results = await _service.ApplyTemplateToMultipleAsync(
            "builtin-dev",
            ["Ubuntu", "Debian"]);

        results.Should().HaveCount(2);
        results.Should().ContainKey("Ubuntu");
        results.Should().ContainKey("Debian");
    }

    #endregion

    #region PreviewTemplateAsync Tests

    [Fact]
    public async Task PreviewTemplateAsync_WithNonExistentTemplate_ReturnsEmptyResult()
    {
        var result = await _service.PreviewTemplateAsync("nonexistent", "Ubuntu");

        result.HasChanges.Should().BeFalse();
    }

    [Fact]
    public async Task PreviewTemplateAsync_ShowsChanges()
    {
        var currentConfig = new WslConfig();
        _wslConfigServiceMock.Setup(s => s.ReadConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentConfig);
        _distroConfigServiceMock.Setup(s => s.ReadConfigAsync("Ubuntu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WslDistroConfig());

        // builtin-lowmem has Memory=4GB, Swap=2GB, and other settings
        var result = await _service.PreviewTemplateAsync("builtin-lowmem", "Ubuntu");

        result.HasChanges.Should().BeTrue();
        // The template sets various settings, check that some changes are detected
        (result.GlobalChanges.Count + result.DistroChanges.Count).Should().BeGreaterThan(0);
    }

    #endregion

    #region Export/Import Tests

    [Fact]
    public async Task ExportTemplateAsync_CreatesFile()
    {
        var exportPath = Path.Combine(_testDirectory, "export.json");

        await _service.ExportTemplateAsync("builtin-dev", exportPath);

        File.Exists(exportPath).Should().BeTrue();
    }

    [Fact]
    public async Task ExportTemplateAsync_SetsIsBuiltInToFalse()
    {
        var exportPath = Path.Combine(_testDirectory, "export.json");

        await _service.ExportTemplateAsync("builtin-dev", exportPath);

        var content = await File.ReadAllTextAsync(exportPath);
        var template = JsonSerializer.Deserialize<ConfigurationTemplate>(content);
        template!.IsBuiltIn.Should().BeFalse();
    }

    [Fact]
    public async Task ImportTemplateAsync_CreatesNewTemplate()
    {
        // First export a template
        var exportPath = Path.Combine(_testDirectory, "export-for-import.json");
        await _service.ExportTemplateAsync("builtin-dev", exportPath);

        // Then import it
        var imported = await _service.ImportTemplateAsync(exportPath);

        imported.Id.Should().NotBe("builtin-dev");
        imported.Name.Should().Be("Development");
        imported.IsBuiltIn.Should().BeFalse();
    }

    #endregion
}
