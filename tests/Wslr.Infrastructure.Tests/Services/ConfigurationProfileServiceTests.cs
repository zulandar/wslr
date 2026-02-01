using System.Text.Json;
using Microsoft.Extensions.Logging;
using Wslr.Core.Interfaces;
using Wslr.Core.Models;
using Wslr.Infrastructure.Services;

namespace Wslr.Infrastructure.Tests.Services;

public class ConfigurationProfileServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _profilesPath;
    private readonly Mock<IWslConfigService> _wslConfigServiceMock;
    private readonly Mock<ILogger<ConfigurationProfileService>> _loggerMock;
    private ConfigurationProfileService _service;

    public ConfigurationProfileServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"ConfigProfileServiceTests_{Guid.NewGuid():N}");
        _profilesPath = Path.Combine(_testDirectory, "Profiles");
        Directory.CreateDirectory(_profilesPath);

        _wslConfigServiceMock = new Mock<IWslConfigService>();
        _loggerMock = new Mock<ILogger<ConfigurationProfileService>>();

        _service = CreateServiceWithCustomPath();
    }

    private ConfigurationProfileService CreateServiceWithCustomPath()
    {
        var original = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        try
        {
            Environment.SetEnvironmentVariable("LOCALAPPDATA", _testDirectory);
            return new ConfigurationProfileService(_wslConfigServiceMock.Object, _loggerMock.Object);
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
        var act = () => new ConfigurationProfileService(null!, _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("wslConfigService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ConfigurationProfileService(_wslConfigServiceMock.Object, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region GetBuiltInProfiles Tests

    [Fact]
    public void GetBuiltInProfiles_ReturnsExpectedProfiles()
    {
        var profiles = _service.GetBuiltInProfiles();

        profiles.Should().HaveCount(4);
        profiles.Should().Contain(p => p.Name == "Balanced");
        profiles.Should().Contain(p => p.Name == "High Performance");
        profiles.Should().Contain(p => p.Name == "Low Memory");
        profiles.Should().Contain(p => p.Name == "Gaming Mode");
    }

    [Fact]
    public void GetBuiltInProfiles_AllHaveIsBuiltInTrue()
    {
        var profiles = _service.GetBuiltInProfiles();

        profiles.Should().OnlyContain(p => p.IsBuiltIn);
    }

    [Fact]
    public void GetBuiltInProfiles_AllHaveKnownIds()
    {
        var profiles = _service.GetBuiltInProfiles();

        profiles.Should().Contain(p => p.Id == "profile-balanced");
        profiles.Should().Contain(p => p.Id == "profile-performance");
        profiles.Should().Contain(p => p.Id == "profile-lowmem");
        profiles.Should().Contain(p => p.Id == "profile-gaming");
    }

    #endregion

    #region GetAllProfilesAsync Tests

    [Fact]
    public async Task GetAllProfilesAsync_IncludesBuiltInProfiles()
    {
        var profiles = await _service.GetAllProfilesAsync();

        profiles.Should().Contain(p => p.IsBuiltIn);
        profiles.Count(p => p.IsBuiltIn).Should().Be(4);
    }

    [Fact]
    public async Task GetAllProfilesAsync_IncludesUserProfiles()
    {
        // Create via service
        var created = await _service.CreateProfileAsync(new ConfigurationProfile
        {
            Name = "My Profile",
            Settings = new WslConfig()
        });

        var profiles = await _service.GetAllProfilesAsync();

        profiles.Should().Contain(p => p.Name == "My Profile");
    }

    [Fact]
    public async Task GetAllProfilesAsync_SortsBuiltInFirst()
    {
        // Create via service
        await _service.CreateProfileAsync(new ConfigurationProfile
        {
            Name = "AAA Profile",
            Settings = new WslConfig()
        });

        var profiles = await _service.GetAllProfilesAsync();
        var firstUserProfileIndex = profiles.ToList().FindIndex(p => !p.IsBuiltIn);

        // All built-in should come before first user profile
        if (firstUserProfileIndex > 0)
        {
            profiles.Take(firstUserProfileIndex).Should().OnlyContain(p => p.IsBuiltIn);
        }
    }

    #endregion

    #region GetProfileAsync Tests

    [Fact]
    public async Task GetProfileAsync_WithBuiltInId_ReturnsBuiltInProfile()
    {
        var profile = await _service.GetProfileAsync("profile-balanced");

        profile.Should().NotBeNull();
        profile!.Name.Should().Be("Balanced");
        profile.IsBuiltIn.Should().BeTrue();
    }

    [Fact]
    public async Task GetProfileAsync_WithNonExistentId_ReturnsNull()
    {
        var profile = await _service.GetProfileAsync("nonexistent");

        profile.Should().BeNull();
    }

    [Fact]
    public async Task GetProfileAsync_WithUserProfileId_ReturnsProfile()
    {
        // Create via service
        var created = await _service.CreateProfileAsync(new ConfigurationProfile
        {
            Name = "User Profile",
            Settings = new WslConfig()
        });

        var profile = await _service.GetProfileAsync(created.Id);

        profile.Should().NotBeNull();
        profile!.Name.Should().Be("User Profile");
    }

    #endregion

    #region CreateProfileAsync Tests

    [Fact]
    public async Task CreateProfileAsync_CreatesNewProfile()
    {
        var profile = new ConfigurationProfile
        {
            Name = "New Profile",
            Settings = new WslConfig()
        };

        var created = await _service.CreateProfileAsync(profile);

        created.Id.Should().HaveLength(8);
        created.Name.Should().Be("New Profile");
        created.IsBuiltIn.Should().BeFalse();
    }

    [Fact]
    public async Task CreateProfileAsync_WithBuiltInTrue_SetsToFalse()
    {
        var profile = new ConfigurationProfile
        {
            Name = "New Profile",
            IsBuiltIn = true,
            Settings = new WslConfig()
        };

        var act = () => _service.CreateProfileAsync(profile);

        // Should throw because we can't create a built-in profile
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateProfileAsync_SetsTimestamps()
    {
        var before = DateTime.UtcNow;
        var profile = new ConfigurationProfile
        {
            Name = "New Profile",
            Settings = new WslConfig()
        };

        var created = await _service.CreateProfileAsync(profile);
        var after = DateTime.UtcNow;

        created.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        created.ModifiedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task CreateProfileAsync_CanBeRetrievedAfterCreation()
    {
        var profile = new ConfigurationProfile
        {
            Name = "Persistent Profile",
            Settings = new WslConfig()
        };

        var created = await _service.CreateProfileAsync(profile);
        var retrieved = await _service.GetProfileAsync(created.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Persistent Profile");
    }

    #endregion

    #region CreateProfileFromCurrentAsync Tests

    [Fact]
    public async Task CreateProfileFromCurrentAsync_CapturesCurrentSettings()
    {
        var currentConfig = new WslConfig
        {
            Wsl2 = new Wsl2Settings { Memory = "8GB", Processors = 4 }
        };
        _wslConfigServiceMock.Setup(s => s.ReadConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentConfig);

        var created = await _service.CreateProfileFromCurrentAsync("Captured Profile");

        created.Name.Should().Be("Captured Profile");
        created.Settings.Wsl2.Memory.Should().Be("8GB");
        created.Settings.Wsl2.Processors.Should().Be(4);
    }

    [Fact]
    public async Task CreateProfileFromCurrentAsync_SetsDefaultDescription()
    {
        _wslConfigServiceMock.Setup(s => s.ReadConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WslConfig());

        var created = await _service.CreateProfileFromCurrentAsync("My Profile");

        created.Description.Should().Contain("current settings");
    }

    [Fact]
    public async Task CreateProfileFromCurrentAsync_UsesProvidedDescription()
    {
        _wslConfigServiceMock.Setup(s => s.ReadConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WslConfig());

        var created = await _service.CreateProfileFromCurrentAsync("My Profile", "Custom description");

        created.Description.Should().Be("Custom description");
    }

    #endregion

    #region UpdateProfileAsync Tests

    [Fact]
    public async Task UpdateProfileAsync_WithBuiltInProfile_ThrowsInvalidOperationException()
    {
        var builtIn = new ConfigurationProfile
        {
            Id = "profile-balanced",
            Name = "Modified Balanced",
            Settings = new WslConfig()
        };

        var act = () => _service.UpdateProfileAsync(builtIn);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*built-in*");
    }

    [Fact]
    public async Task UpdateProfileAsync_UpdatesModifiedAt()
    {
        var original = await _service.CreateProfileAsync(new ConfigurationProfile
        {
            Name = "Original",
            Settings = new WslConfig()
        });

        await Task.Delay(100);

        var updated = await _service.UpdateProfileAsync(original with { Name = "Updated" });

        updated.ModifiedAt.Should().BeAfter(original.ModifiedAt);
    }

    #endregion

    #region DeleteProfileAsync Tests

    [Fact]
    public async Task DeleteProfileAsync_WithBuiltInProfile_ThrowsInvalidOperationException()
    {
        var act = () => _service.DeleteProfileAsync("profile-balanced");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*built-in*");
    }

    [Fact]
    public async Task DeleteProfileAsync_WithNonExistentProfile_ReturnsFalse()
    {
        var result = await _service.DeleteProfileAsync("nonexistent");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteProfileAsync_WithUserProfile_DeletesAndReturnsTrue()
    {
        var created = await _service.CreateProfileAsync(new ConfigurationProfile
        {
            Name = "To Delete",
            Settings = new WslConfig()
        });

        var result = await _service.DeleteProfileAsync(created.Id);

        result.Should().BeTrue();
        var retrieved = await _service.GetProfileAsync(created.Id);
        retrieved.Should().BeNull();
    }

    #endregion

    #region DuplicateProfileAsync Tests

    [Fact]
    public async Task DuplicateProfileAsync_CreatesNewProfile()
    {
        var duplicate = await _service.DuplicateProfileAsync("profile-balanced");

        duplicate.Id.Should().NotBe("profile-balanced");
        duplicate.IsBuiltIn.Should().BeFalse();
        duplicate.Name.Should().Be("Balanced (Copy)");
    }

    [Fact]
    public async Task DuplicateProfileAsync_WithCustomName_UsesCustomName()
    {
        var duplicate = await _service.DuplicateProfileAsync("profile-balanced", "My Balanced Copy");

        duplicate.Name.Should().Be("My Balanced Copy");
    }

    [Fact]
    public async Task DuplicateProfileAsync_WithNonExistentId_ThrowsInvalidOperationException()
    {
        var act = () => _service.DuplicateProfileAsync("nonexistent");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task DuplicateProfileAsync_CopiesSettings()
    {
        var original = await _service.GetProfileAsync("profile-performance");
        var duplicate = await _service.DuplicateProfileAsync("profile-performance");

        duplicate.Settings.Wsl2.Memory.Should().Be(original!.Settings.Wsl2.Memory);
        duplicate.Settings.Wsl2.Processors.Should().Be(original.Settings.Wsl2.Processors);
    }

    #endregion

    #region SwitchToProfileAsync Tests

    [Fact]
    public async Task SwitchToProfileAsync_WithNonExistentProfile_ReturnsFailure()
    {
        var result = await _service.SwitchToProfileAsync("nonexistent");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task SwitchToProfileAsync_CreatesBackupAndWritesConfig()
    {
        _wslConfigServiceMock.Setup(s => s.CreateBackupAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("backup.txt");
        _wslConfigServiceMock.Setup(s => s.WriteConfigAsync(It.IsAny<WslConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.SwitchToProfileAsync("profile-balanced");

        result.Success.Should().BeTrue();
        result.RestartRequired.Should().BeTrue();
        _wslConfigServiceMock.Verify(s => s.CreateBackupAsync(It.IsAny<CancellationToken>()), Times.Once);
        _wslConfigServiceMock.Verify(s => s.WriteConfigAsync(It.IsAny<WslConfig>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SwitchToProfileAsync_WhenWriteFails_ReturnsFailure()
    {
        _wslConfigServiceMock.Setup(s => s.CreateBackupAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("backup.txt");
        _wslConfigServiceMock.Setup(s => s.WriteConfigAsync(It.IsAny<WslConfig>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Write failed"));

        var result = await _service.SwitchToProfileAsync("profile-balanced");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Write failed");
    }

    [Fact]
    public async Task SwitchToProfileAsync_UpdatesActiveProfileId()
    {
        _wslConfigServiceMock.Setup(s => s.CreateBackupAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        _wslConfigServiceMock.Setup(s => s.WriteConfigAsync(It.IsAny<WslConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.SwitchToProfileAsync("profile-balanced");

        _service.GetActiveProfileId().Should().Be("profile-balanced");
    }

    #endregion

    #region CompareProfilesAsync Tests

    [Fact]
    public async Task CompareProfilesAsync_WithIdenticalProfiles_ReturnsEmptyDifferences()
    {
        var differences = await _service.CompareProfilesAsync("profile-balanced", "profile-balanced");

        differences.Should().BeEmpty();
    }

    [Fact]
    public async Task CompareProfilesAsync_WithDifferentProfiles_ReturnsDifferences()
    {
        var differences = await _service.CompareProfilesAsync("profile-balanced", "profile-performance");

        differences.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CompareProfilesAsync_WithNonExistentProfile_ThrowsInvalidOperationException()
    {
        var act = () => _service.CompareProfilesAsync("profile-balanced", "nonexistent");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region Export/Import Tests

    [Fact]
    public async Task ExportProfileAsync_CreatesFile()
    {
        var exportPath = Path.Combine(_testDirectory, "export.json");

        await _service.ExportProfileAsync("profile-balanced", exportPath);

        File.Exists(exportPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(exportPath);
        content.Should().Contain("Balanced");
    }

    [Fact]
    public async Task ExportProfileAsync_SetsIsBuiltInToFalse()
    {
        var exportPath = Path.Combine(_testDirectory, "export.json");

        await _service.ExportProfileAsync("profile-balanced", exportPath);

        var content = await File.ReadAllTextAsync(exportPath);
        var profile = JsonSerializer.Deserialize<ConfigurationProfile>(content);
        profile!.IsBuiltIn.Should().BeFalse();
    }

    [Fact]
    public async Task ImportProfileAsync_CreatesNewProfile()
    {
        // First export a profile
        var exportPath = Path.Combine(_testDirectory, "export-for-import.json");
        await _service.ExportProfileAsync("profile-balanced", exportPath);

        // Then import it
        var imported = await _service.ImportProfileAsync(exportPath);

        imported.Id.Should().NotBe("profile-balanced");
        imported.Name.Should().Be("Balanced");
        imported.IsBuiltIn.Should().BeFalse();
    }

    #endregion

    #region ActiveProfileChanged Event Tests

    [Fact]
    public async Task SwitchToProfileAsync_RaisesActiveProfileChangedEvent()
    {
        _wslConfigServiceMock.Setup(s => s.CreateBackupAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        _wslConfigServiceMock.Setup(s => s.WriteConfigAsync(It.IsAny<WslConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        string? eventProfileId = null;
        _service.ActiveProfileChanged += (_, id) => eventProfileId = id;

        await _service.SwitchToProfileAsync("profile-balanced");

        eventProfileId.Should().Be("profile-balanced");
    }

    #endregion
}
