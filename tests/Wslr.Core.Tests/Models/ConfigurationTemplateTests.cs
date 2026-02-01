using Wslr.Core.Models;

namespace Wslr.Core.Tests.Models;

public class ConfigurationTemplateTests
{
    #region Record Initialization Tests

    [Fact]
    public void Constructor_WithDefaults_SetsIdToEightCharGuid()
    {
        var template = new ConfigurationTemplate();

        template.Id.Should().HaveLength(8);
        template.Id.Should().MatchRegex("^[a-f0-9]{8}$");
    }

    [Fact]
    public void Constructor_WithDefaults_SetsNameToEmptyString()
    {
        var template = new ConfigurationTemplate();

        template.Name.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithDefaults_SetsDescriptionToNull()
    {
        var template = new ConfigurationTemplate();

        template.Description.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithDefaults_SetsIsBuiltInToFalse()
    {
        var template = new ConfigurationTemplate();

        template.IsBuiltIn.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithDefaults_SetsGlobalSettingsToNull()
    {
        var template = new ConfigurationTemplate();

        template.GlobalSettings.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithDefaults_SetsDistroSettingsToNull()
    {
        var template = new ConfigurationTemplate();

        template.DistroSettings.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithDefaults_SetsTimestampsToUtcNow()
    {
        var before = DateTime.UtcNow;
        var template = new ConfigurationTemplate();
        var after = DateTime.UtcNow;

        template.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        template.ModifiedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    #endregion

    #region With Expression Tests

    [Fact]
    public void With_CreatesNewTemplatePreservingOtherProperties()
    {
        var original = new ConfigurationTemplate
        {
            Id = "test1234",
            Name = "Original",
            Description = "Test",
            IsBuiltIn = true
        };

        var updated = original with { Name = "Updated" };

        original.Name.Should().Be("Original");
        updated.Name.Should().Be("Updated");
        updated.Id.Should().Be("test1234");
        updated.Description.Should().Be("Test");
        updated.IsBuiltIn.Should().BeTrue();
    }

    #endregion
}

public class TemplateApplyResultTests
{
    #region Static Factory Method Tests

    [Fact]
    public void Succeeded_WithBothApplied_ReturnsSuccessWithRestartRequired()
    {
        var result = TemplateApplyResult.Succeeded(globalApplied: true, distroApplied: true);

        result.Success.Should().BeTrue();
        result.GlobalSettingsApplied.Should().BeTrue();
        result.DistroSettingsApplied.Should().BeTrue();
        result.RestartRequired.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Succeeded_WithOnlyGlobalApplied_ReturnsRestartRequired()
    {
        var result = TemplateApplyResult.Succeeded(globalApplied: true, distroApplied: false);

        result.Success.Should().BeTrue();
        result.GlobalSettingsApplied.Should().BeTrue();
        result.DistroSettingsApplied.Should().BeFalse();
        result.RestartRequired.Should().BeTrue();
    }

    [Fact]
    public void Succeeded_WithOnlyDistroApplied_ReturnsRestartRequired()
    {
        var result = TemplateApplyResult.Succeeded(globalApplied: false, distroApplied: true);

        result.Success.Should().BeTrue();
        result.GlobalSettingsApplied.Should().BeFalse();
        result.DistroSettingsApplied.Should().BeTrue();
        result.RestartRequired.Should().BeTrue();
    }

    [Fact]
    public void Succeeded_WithNeitherApplied_ReturnsNoRestartRequired()
    {
        var result = TemplateApplyResult.Succeeded(globalApplied: false, distroApplied: false);

        result.Success.Should().BeTrue();
        result.RestartRequired.Should().BeFalse();
    }

    [Fact]
    public void Failed_WithMessage_ReturnsFailureResult()
    {
        var result = TemplateApplyResult.Failed("Template not found");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Template not found");
        result.GlobalSettingsApplied.Should().BeFalse();
        result.DistroSettingsApplied.Should().BeFalse();
        result.RestartRequired.Should().BeFalse();
    }

    #endregion

    #region Default Value Tests

    [Fact]
    public void DefaultConstructor_SetsAllBoolsToFalse()
    {
        var result = new TemplateApplyResult();

        result.Success.Should().BeFalse();
        result.GlobalSettingsApplied.Should().BeFalse();
        result.DistroSettingsApplied.Should().BeFalse();
        result.RestartRequired.Should().BeFalse();
    }

    #endregion
}

public class TemplateApplyOptionsTests
{
    [Fact]
    public void Constructor_WithDefaults_SetsApplyGlobalSettingsToTrue()
    {
        var options = new TemplateApplyOptions();

        options.ApplyGlobalSettings.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithDefaults_SetsApplyDistroSettingsToTrue()
    {
        var options = new TemplateApplyOptions();

        options.ApplyDistroSettings.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithDefaults_SetsMergeModeToMerge()
    {
        var options = new TemplateApplyOptions();

        options.MergeMode.Should().Be(TemplateMergeMode.Merge);
    }

    [Fact]
    public void WithOverwriteMode_SetsCorrectly()
    {
        var options = new TemplateApplyOptions
        {
            MergeMode = TemplateMergeMode.Overwrite
        };

        options.MergeMode.Should().Be(TemplateMergeMode.Overwrite);
    }

    [Fact]
    public void CanDisableBothSettings()
    {
        var options = new TemplateApplyOptions
        {
            ApplyGlobalSettings = false,
            ApplyDistroSettings = false
        };

        options.ApplyGlobalSettings.Should().BeFalse();
        options.ApplyDistroSettings.Should().BeFalse();
    }
}

public class TemplateMergeModeTests
{
    [Fact]
    public void Merge_HasValueZero()
    {
        ((int)TemplateMergeMode.Merge).Should().Be(0);
    }

    [Fact]
    public void Overwrite_HasValueOne()
    {
        ((int)TemplateMergeMode.Overwrite).Should().Be(1);
    }

    [Theory]
    [InlineData(TemplateMergeMode.Merge, "Merge")]
    [InlineData(TemplateMergeMode.Overwrite, "Overwrite")]
    public void ToString_ReturnsExpectedName(TemplateMergeMode mode, string expected)
    {
        mode.ToString().Should().Be(expected);
    }
}
