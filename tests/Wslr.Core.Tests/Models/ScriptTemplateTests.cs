using Wslr.Core.Models;

namespace Wslr.Core.Tests.Models;

public class ScriptTemplateTests
{
    #region Record Initialization Tests

    [Fact]
    public void Constructor_WithDefaults_SetsIdToEightCharGuid()
    {
        var template = new ScriptTemplate
        {
            Name = "Test",
            ScriptContent = "echo test"
        };

        template.Id.Should().HaveLength(8);
        template.Id.Should().MatchRegex("^[a-f0-9]{8}$");
    }

    [Fact]
    public void Constructor_WithDefaults_SetsDescriptionToNull()
    {
        var template = new ScriptTemplate
        {
            Name = "Test",
            ScriptContent = "echo test"
        };

        template.Description.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithDefaults_SetsCategoryToNull()
    {
        var template = new ScriptTemplate
        {
            Name = "Test",
            ScriptContent = "echo test"
        };

        template.Category.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithDefaults_SetsIsBuiltInToFalse()
    {
        var template = new ScriptTemplate
        {
            Name = "Test",
            ScriptContent = "echo test"
        };

        template.IsBuiltIn.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithDefaults_SetsVariablesToNull()
    {
        var template = new ScriptTemplate
        {
            Name = "Test",
            ScriptContent = "echo test"
        };

        template.Variables.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithDefaults_SetsTimestampsToUtcNow()
    {
        var before = DateTime.UtcNow;
        var template = new ScriptTemplate
        {
            Name = "Test",
            ScriptContent = "echo test"
        };
        var after = DateTime.UtcNow;

        template.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        template.ModifiedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    #endregion

    #region Required Property Tests

    [Fact]
    public void Name_IsRequired_CanBeSet()
    {
        var template = new ScriptTemplate
        {
            Name = "Development Setup",
            ScriptContent = "echo test"
        };

        template.Name.Should().Be("Development Setup");
    }

    [Fact]
    public void ScriptContent_IsRequired_CanBeSet()
    {
        var scriptContent = """
            #!/bin/bash
            echo "Hello World"
            """;

        var template = new ScriptTemplate
        {
            Name = "Test",
            ScriptContent = scriptContent
        };

        template.ScriptContent.Should().Be(scriptContent);
    }

    #endregion

    #region Variables Tests

    [Fact]
    public void Variables_CanBeInitializedWithDictionary()
    {
        var template = new ScriptTemplate
        {
            Name = "Test",
            ScriptContent = "echo ${USERNAME}",
            Variables = new Dictionary<string, string>
            {
                ["USERNAME"] = "defaultuser",
                ["HOME_DIR"] = "/home/user"
            }
        };

        template.Variables.Should().HaveCount(2);
        template.Variables!["USERNAME"].Should().Be("defaultuser");
        template.Variables["HOME_DIR"].Should().Be("/home/user");
    }

    [Fact]
    public void Variables_WithEmptyDictionary_IsEmpty()
    {
        var template = new ScriptTemplate
        {
            Name = "Test",
            ScriptContent = "echo test",
            Variables = new Dictionary<string, string>()
        };

        template.Variables.Should().BeEmpty();
    }

    #endregion

    #region With Expression Tests

    [Fact]
    public void With_CreatesNewTemplateWithUpdatedName()
    {
        var original = new ScriptTemplate
        {
            Id = "test1234",
            Name = "Original",
            ScriptContent = "echo test",
            Category = "Development"
        };

        var updated = original with { Name = "Updated" };

        original.Name.Should().Be("Original");
        updated.Name.Should().Be("Updated");
        updated.Id.Should().Be("test1234");
        updated.Category.Should().Be("Development");
    }

    [Fact]
    public void With_CanUpdateScriptContent()
    {
        var original = new ScriptTemplate
        {
            Name = "Test",
            ScriptContent = "echo original"
        };

        var updated = original with { ScriptContent = "echo updated" };

        original.ScriptContent.Should().Be("echo original");
        updated.ScriptContent.Should().Be("echo updated");
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        var createdAt = DateTime.UtcNow;
        var template1 = new ScriptTemplate
        {
            Id = "test1234",
            Name = "Test",
            ScriptContent = "echo test",
            CreatedAt = createdAt,
            ModifiedAt = createdAt
        };
        var template2 = new ScriptTemplate
        {
            Id = "test1234",
            Name = "Test",
            ScriptContent = "echo test",
            CreatedAt = createdAt,
            ModifiedAt = createdAt
        };

        template1.Should().Be(template2);
    }

    [Fact]
    public void Equals_WithDifferentScriptContent_ReturnsFalse()
    {
        var template1 = new ScriptTemplate
        {
            Id = "test1234",
            Name = "Test",
            ScriptContent = "echo test1"
        };
        var template2 = new ScriptTemplate
        {
            Id = "test1234",
            Name = "Test",
            ScriptContent = "echo test2"
        };

        template1.Should().NotBe(template2);
    }

    #endregion
}
