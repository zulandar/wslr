using System.Text.Json;
using Microsoft.Extensions.Logging;
using Wslr.Core.Models;
using Wslr.Infrastructure.Services;

namespace Wslr.Infrastructure.Tests.Services;

public class ScriptTemplateServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _templatesPath;
    private readonly Mock<ILogger<ScriptTemplateService>> _loggerMock;
    private readonly ScriptTemplateService _service;

    public ScriptTemplateServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"ScriptTemplateServiceTests_{Guid.NewGuid():N}");
        _templatesPath = Path.Combine(_testDirectory, "ScriptTemplates");
        Directory.CreateDirectory(_templatesPath);

        _loggerMock = new Mock<ILogger<ScriptTemplateService>>();

        // Create service using reflection to set custom path (or use environment variable approach)
        // For testing, we'll create a wrapper that allows custom path
        _service = CreateServiceWithCustomPath();
    }

    private ScriptTemplateService CreateServiceWithCustomPath()
    {
        // Store original and set test path
        var original = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        try
        {
            Environment.SetEnvironmentVariable("LOCALAPPDATA", _testDirectory);
            return new ScriptTemplateService(_loggerMock.Object);
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
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ScriptTemplateService(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region GetAllTemplatesAsync Tests

    [Fact]
    public async Task GetAllTemplatesAsync_ReturnsBuiltInTemplates()
    {
        var templates = await _service.GetAllTemplatesAsync();

        templates.Should().NotBeEmpty();
        templates.Should().Contain(t => t.IsBuiltIn);
        templates.Should().Contain(t => t.Name == "Development Environment");
        templates.Should().Contain(t => t.Name == "Node.js Development");
        templates.Should().Contain(t => t.Name == "Python Development");
    }

    [Fact]
    public async Task GetAllTemplatesAsync_ReturnsBuiltInTemplatesFirst()
    {
        var templates = await _service.GetAllTemplatesAsync();

        var builtInTemplates = templates.Where(t => t.IsBuiltIn).ToList();
        var userTemplates = templates.Where(t => !t.IsBuiltIn).ToList();

        if (builtInTemplates.Any() && userTemplates.Any())
        {
            var lastBuiltInIndex = templates.ToList().IndexOf(builtInTemplates.Last());
            var firstUserIndex = templates.ToList().IndexOf(userTemplates.First());
            lastBuiltInIndex.Should().BeLessThan(firstUserIndex);
        }
    }

    [Fact]
    public async Task GetAllTemplatesAsync_IncludesUserTemplates()
    {
        // Create a user template via the service
        var created = await _service.CreateTemplateAsync(new ScriptTemplate
        {
            Name = "User Template",
            ScriptContent = "echo user"
        });

        var templates = await _service.GetAllTemplatesAsync();

        templates.Should().Contain(t => t.Name == "User Template");
    }

    #endregion

    #region GetTemplatesByCategoryAsync Tests

    [Fact]
    public async Task GetTemplatesByCategoryAsync_FiltersByCategory()
    {
        var templates = await _service.GetTemplatesByCategoryAsync("Development");

        templates.Should().NotBeEmpty();
        templates.Should().OnlyContain(t => t.Category == "Development");
    }

    [Fact]
    public async Task GetTemplatesByCategoryAsync_WithNonExistentCategory_ReturnsEmpty()
    {
        var templates = await _service.GetTemplatesByCategoryAsync("NonExistent");

        templates.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTemplatesByCategoryAsync_IsCaseInsensitive()
    {
        var templates1 = await _service.GetTemplatesByCategoryAsync("Development");
        var templates2 = await _service.GetTemplatesByCategoryAsync("development");

        templates1.Should().BeEquivalentTo(templates2);
    }

    #endregion

    #region GetTemplateAsync Tests

    [Fact]
    public async Task GetTemplateAsync_WithBuiltInId_ReturnsBuiltInTemplate()
    {
        var template = await _service.GetTemplateAsync("builtin01");

        template.Should().NotBeNull();
        template!.IsBuiltIn.Should().BeTrue();
        template.Name.Should().Be("Development Environment");
    }

    [Fact]
    public async Task GetTemplateAsync_WithNonExistentId_ReturnsNull()
    {
        var template = await _service.GetTemplateAsync("nonexistent");

        template.Should().BeNull();
    }

    [Fact]
    public async Task GetTemplateAsync_WithNullId_ThrowsArgumentException()
    {
        var act = () => _service.GetTemplateAsync(null!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetTemplateAsync_WithUserTemplate_ReturnsTemplate()
    {
        // Create a user template via the service
        var created = await _service.CreateTemplateAsync(new ScriptTemplate
        {
            Name = "User Template",
            ScriptContent = "echo test"
        });

        var result = await _service.GetTemplateAsync(created.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("User Template");
    }

    #endregion

    #region CreateTemplateAsync Tests

    [Fact]
    public async Task CreateTemplateAsync_CreatesNewTemplate()
    {
        var template = new ScriptTemplate
        {
            Name = "New Template",
            ScriptContent = "echo new"
        };

        var created = await _service.CreateTemplateAsync(template);

        created.Id.Should().HaveLength(8);
        created.Name.Should().Be("New Template");
        created.IsBuiltIn.Should().BeFalse();
    }

    [Fact]
    public async Task CreateTemplateAsync_SetsTimestamps()
    {
        var before = DateTime.UtcNow;
        var template = new ScriptTemplate
        {
            Name = "New Template",
            ScriptContent = "echo test"
        };

        var created = await _service.CreateTemplateAsync(template);
        var after = DateTime.UtcNow;

        created.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        created.ModifiedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task CreateTemplateAsync_CanBeRetrievedAfterCreation()
    {
        var template = new ScriptTemplate
        {
            Name = "Persistent Template",
            ScriptContent = "echo persist"
        };

        var created = await _service.CreateTemplateAsync(template);
        var retrieved = await _service.GetTemplateAsync(created.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Persistent Template");
    }

    [Fact]
    public async Task CreateTemplateAsync_WithNullTemplate_ThrowsArgumentNullException()
    {
        var act = () => _service.CreateTemplateAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region UpdateTemplateAsync Tests

    [Fact]
    public async Task UpdateTemplateAsync_WithBuiltInTemplate_ThrowsInvalidOperationException()
    {
        var builtIn = new ScriptTemplate
        {
            Id = "builtin01",
            Name = "Modified",
            ScriptContent = "echo test"
        };

        var act = () => _service.UpdateTemplateAsync(builtIn);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*built-in*");
    }

    [Fact]
    public async Task UpdateTemplateAsync_WithNonExistentTemplate_ThrowsInvalidOperationException()
    {
        var template = new ScriptTemplate
        {
            Id = "nonexistent",
            Name = "Test",
            ScriptContent = "echo test"
        };

        var act = () => _service.UpdateTemplateAsync(template);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task UpdateTemplateAsync_UpdatesModifiedAt()
    {
        var original = await _service.CreateTemplateAsync(new ScriptTemplate
        {
            Name = "Original",
            ScriptContent = "echo original"
        });

        await Task.Delay(100); // Ensure time difference

        var updated = await _service.UpdateTemplateAsync(original with { Name = "Updated" });

        updated.ModifiedAt.Should().BeAfter(original.ModifiedAt);
    }

    #endregion

    #region DeleteTemplateAsync Tests

    [Fact]
    public async Task DeleteTemplateAsync_WithBuiltInTemplate_ThrowsInvalidOperationException()
    {
        var act = () => _service.DeleteTemplateAsync("builtin01");

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
        var created = await _service.CreateTemplateAsync(new ScriptTemplate
        {
            Name = "To Delete",
            ScriptContent = "echo delete"
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
        var duplicate = await _service.DuplicateTemplateAsync("builtin01");

        duplicate.Id.Should().NotBe("builtin01");
        duplicate.IsBuiltIn.Should().BeFalse();
        duplicate.Name.Should().Be("Development Environment (Copy)");
    }

    [Fact]
    public async Task DuplicateTemplateAsync_WithCustomName_UsesCustomName()
    {
        var duplicate = await _service.DuplicateTemplateAsync("builtin01", "My Custom Copy");

        duplicate.Name.Should().Be("My Custom Copy");
    }

    [Fact]
    public async Task DuplicateTemplateAsync_WithNonExistentId_ThrowsInvalidOperationException()
    {
        var act = () => _service.DuplicateTemplateAsync("nonexistent");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task DuplicateTemplateAsync_CopiesScriptContent()
    {
        var original = await _service.GetTemplateAsync("builtin01");
        var duplicate = await _service.DuplicateTemplateAsync("builtin01");

        duplicate.ScriptContent.Should().Be(original!.ScriptContent);
    }

    #endregion

    #region ExportTemplateAsync Tests

    [Fact]
    public async Task ExportTemplateAsync_ReturnsValidJson()
    {
        var json = await _service.ExportTemplateAsync("builtin01");

        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("Development Environment");
        json.Should().Contain("isBuiltIn");
    }

    [Fact]
    public async Task ExportTemplateAsync_SetsIsBuiltInToFalse()
    {
        var json = await _service.ExportTemplateAsync("builtin01");

        // isBuiltIn should be false in the export (camelCase JSON)
        json.Should().MatchRegex("\"isBuiltIn\"\\s*:\\s*false");
    }

    [Fact]
    public async Task ExportTemplateAsync_WithNonExistentId_ThrowsInvalidOperationException()
    {
        var act = () => _service.ExportTemplateAsync("nonexistent");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region ImportTemplateAsync Tests

    [Fact]
    public async Task ImportTemplateAsync_CreatesNewTemplate()
    {
        // First export a built-in template
        var json = await _service.ExportTemplateAsync("builtin01");

        // Then import it
        var imported = await _service.ImportTemplateAsync(json);

        imported.Id.Should().NotBe("builtin01");
        imported.Name.Should().Be("Development Environment");
        imported.IsBuiltIn.Should().BeFalse();
    }

    [Fact]
    public async Task ImportTemplateAsync_WithInvalidJson_ThrowsInvalidOperationException()
    {
        var act = () => _service.ImportTemplateAsync("not valid json");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid*");
    }

    [Fact]
    public async Task ImportTemplateAsync_WithEmptyJson_ThrowsArgumentException()
    {
        var act = () => _service.ImportTemplateAsync("");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ImportTemplateAsync_SetsNewTimestamps()
    {
        var before = DateTime.UtcNow;
        var json = await _service.ExportTemplateAsync("builtin01");

        var imported = await _service.ImportTemplateAsync(json);
        var after = DateTime.UtcNow;

        imported.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        imported.ModifiedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    #endregion
}
