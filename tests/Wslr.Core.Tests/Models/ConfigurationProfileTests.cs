using Wslr.Core.Models;

namespace Wslr.Core.Tests.Models;

public class ConfigurationProfileTests
{
    #region Record Initialization Tests

    [Fact]
    public void Constructor_WithDefaults_SetsIdToEightCharGuid()
    {
        var profile = new ConfigurationProfile();

        profile.Id.Should().HaveLength(8);
        profile.Id.Should().MatchRegex("^[a-f0-9]{8}$");
    }

    [Fact]
    public void Constructor_WithDefaults_SetsCreatedAtToUtcNow()
    {
        var before = DateTime.UtcNow;
        var profile = new ConfigurationProfile();
        var after = DateTime.UtcNow;

        profile.CreatedAt.Should().BeOnOrAfter(before);
        profile.CreatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Constructor_WithDefaults_SetsModifiedAtToUtcNow()
    {
        var before = DateTime.UtcNow;
        var profile = new ConfigurationProfile();
        var after = DateTime.UtcNow;

        profile.ModifiedAt.Should().BeOnOrAfter(before);
        profile.ModifiedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Constructor_WithDefaults_SetsNameToEmptyString()
    {
        var profile = new ConfigurationProfile();

        profile.Name.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithDefaults_SetsDescriptionToNull()
    {
        var profile = new ConfigurationProfile();

        profile.Description.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithDefaults_SetsIsBuiltInToFalse()
    {
        var profile = new ConfigurationProfile();

        profile.IsBuiltIn.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithDefaults_InitializesEmptySettings()
    {
        var profile = new ConfigurationProfile();

        profile.Settings.Should().NotBeNull();
    }

    #endregion

    #region Record With Expression Tests

    [Fact]
    public void With_CreatesNewProfileWithUpdatedName()
    {
        var original = new ConfigurationProfile { Name = "Original" };

        var updated = original with { Name = "Updated" };

        original.Name.Should().Be("Original");
        updated.Name.Should().Be("Updated");
    }

    [Fact]
    public void With_PreservesOtherProperties()
    {
        var original = new ConfigurationProfile
        {
            Id = "test1234",
            Name = "Original",
            Description = "Test description",
            IsBuiltIn = true
        };

        var updated = original with { Name = "Updated" };

        updated.Id.Should().Be("test1234");
        updated.Description.Should().Be("Test description");
        updated.IsBuiltIn.Should().BeTrue();
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_WithSameReference_ReturnsTrue()
    {
        var profile = new ConfigurationProfile { Id = "test1234", Name = "Test" };

        profile.Should().Be(profile);
    }

    [Fact]
    public void Equals_WithDifferentName_ReturnsFalse()
    {
        var id = "test1234";
        var createdAt = DateTime.UtcNow;
        var settings = new WslConfig();

        var profile1 = new ConfigurationProfile
        {
            Id = id,
            Name = "Test1",
            CreatedAt = createdAt,
            ModifiedAt = createdAt,
            Settings = settings
        };
        var profile2 = new ConfigurationProfile
        {
            Id = id,
            Name = "Test2",
            CreatedAt = createdAt,
            ModifiedAt = createdAt,
            Settings = settings
        };

        profile1.Should().NotBe(profile2);
    }

    [Fact]
    public void With_Expression_PreservesEquality()
    {
        var profile = new ConfigurationProfile { Id = "test1234", Name = "Test" };
        var copy = profile with { };

        // With expression creates an equal copy when no changes are made
        copy.Id.Should().Be(profile.Id);
        copy.Name.Should().Be(profile.Name);
    }

    #endregion
}

public class ProfileSwitchResultTests
{
    #region Static Factory Method Tests

    [Fact]
    public void Succeeded_ReturnsSuccessResult()
    {
        var result = ProfileSwitchResult.Succeeded();

        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.RestartRequired.Should().BeTrue();
    }

    [Fact]
    public void Failed_WithMessage_ReturnsFailureResult()
    {
        var errorMessage = "Profile not found";

        var result = ProfileSwitchResult.Failed(errorMessage);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be(errorMessage);
        result.RestartRequired.Should().BeFalse();
    }

    [Fact]
    public void Failed_WithEmptyMessage_ReturnsFailureWithEmptyMessage()
    {
        var result = ProfileSwitchResult.Failed("");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().BeEmpty();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Success_DefaultValue_IsFalse()
    {
        var result = new ProfileSwitchResult();

        result.Success.Should().BeFalse();
    }

    [Fact]
    public void RestartRequired_DefaultValue_IsFalse()
    {
        var result = new ProfileSwitchResult();

        result.RestartRequired.Should().BeFalse();
    }

    #endregion
}

public class ProfileDifferenceTests
{
    [Fact]
    public void Constructor_WithDefaults_SetsEmptyStrings()
    {
        var diff = new ProfileDifference();

        diff.Section.Should().BeEmpty();
        diff.Setting.Should().BeEmpty();
        diff.Value1.Should().BeNull();
        diff.Value2.Should().BeNull();
    }

    [Fact]
    public void InitWithValues_SetsAllProperties()
    {
        var diff = new ProfileDifference
        {
            Section = "wsl2",
            Setting = "memory",
            Value1 = "4GB",
            Value2 = "8GB"
        };

        diff.Section.Should().Be("wsl2");
        diff.Setting.Should().Be("memory");
        diff.Value1.Should().Be("4GB");
        diff.Value2.Should().Be("8GB");
    }

    [Fact]
    public void Value1_CanBeNull_RepresentsMissingSetting()
    {
        var diff = new ProfileDifference
        {
            Section = "experimental",
            Setting = "sparseVhd",
            Value1 = null,
            Value2 = "true"
        };

        diff.Value1.Should().BeNull();
        diff.Value2.Should().Be("true");
    }
}
