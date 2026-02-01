using Wslr.Core.Interfaces;
using Wslr.Core.Models;
using Wslr.UI.Services;
using Wslr.UI.ViewModels;
using Microsoft.Extensions.Logging;

namespace Wslr.UI.Tests.ViewModels;

public class ProfileListViewModelTests
{
    private readonly Mock<IConfigurationProfileService> _profileServiceMock;
    private readonly Mock<IWslService> _wslServiceMock;
    private readonly Mock<IDialogService> _dialogServiceMock;
    private readonly Mock<ILogger<ProfileListViewModel>> _loggerMock;

    public ProfileListViewModelTests()
    {
        _profileServiceMock = new Mock<IConfigurationProfileService>();
        _wslServiceMock = new Mock<IWslService>();
        _dialogServiceMock = new Mock<IDialogService>();
        _loggerMock = new Mock<ILogger<ProfileListViewModel>>();
    }

    private ProfileListViewModel CreateViewModel()
    {
        return new ProfileListViewModel(
            _profileServiceMock.Object,
            _wslServiceMock.Object,
            _dialogServiceMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullProfileService_ThrowsArgumentNullException()
    {
        var act = () => new ProfileListViewModel(
            null!,
            _wslServiceMock.Object,
            _dialogServiceMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("profileService");
    }

    [Fact]
    public void Constructor_InitializesWithEmptyState()
    {
        var vm = CreateViewModel();

        vm.Profiles.Should().BeEmpty();
        vm.SelectedProfile.Should().BeNull();
        vm.IsLoading.Should().BeFalse();
        vm.IsEditing.Should().BeFalse();
        vm.IsComparing.Should().BeFalse();
    }

    #endregion

    #region LoadAsync Tests

    [Fact]
    public async Task LoadAsync_LoadsProfiles()
    {
        var profiles = new List<ConfigurationProfile>
        {
            new() { Id = "1", Name = "Profile 1" },
            new() { Id = "2", Name = "Profile 2" }
        };
        _profileServiceMock.Setup(s => s.GetAllProfilesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(profiles);

        var vm = CreateViewModel();
        await vm.LoadAsync();

        vm.Profiles.Should().HaveCount(2);
        vm.Profiles[0].Name.Should().Be("Profile 1");
    }

    [Fact]
    public async Task LoadAsync_SetsActiveProfile()
    {
        var profiles = new List<ConfigurationProfile>
        {
            new() { Id = "1", Name = "Profile 1" },
            new() { Id = "2", Name = "Profile 2" }
        };
        _profileServiceMock.Setup(s => s.GetAllProfilesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(profiles);
        _profileServiceMock.Setup(s => s.GetActiveProfileId()).Returns("2");

        var vm = CreateViewModel();
        await vm.LoadAsync();

        vm.ActiveProfileId.Should().Be("2");
        vm.Profiles[1].IsActive.Should().BeTrue();
        vm.Profiles[0].IsActive.Should().BeFalse();
    }

    #endregion

    #region NewProfile Tests

    [Fact]
    public void NewProfile_EntersEditingMode()
    {
        var vm = CreateViewModel();

        vm.NewProfileCommand.Execute(null);

        vm.IsEditing.Should().BeTrue();
        vm.EditName.Should().Be("New Profile");
        vm.EditDescription.Should().BeEmpty();
        vm.SelectedProfile.Should().BeNull();
    }

    #endregion

    #region SaveProfileAsync Tests

    [Fact]
    public async Task SaveProfileAsync_WithEmptyName_SetsError()
    {
        var vm = CreateViewModel();
        vm.EditName = "";

        await vm.SaveProfileCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Contain("required");
    }

    [Fact]
    public async Task SaveProfileAsync_CreatesNewProfile()
    {
        var createdProfile = new ConfigurationProfile { Id = "new", Name = "New Profile" };
        _profileServiceMock.Setup(s => s.CreateProfileAsync(It.IsAny<ConfigurationProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdProfile);

        var vm = CreateViewModel();
        vm.NewProfileCommand.Execute(null);
        vm.EditName = "New Profile";

        await vm.SaveProfileCommand.ExecuteAsync(null);

        vm.IsEditing.Should().BeFalse();
        vm.SuccessMessage.Should().Contain("saved");
        vm.Profiles.Should().ContainSingle(p => p.Name == "New Profile");
    }

    #endregion

    #region EditProfile Tests

    [Fact]
    public void EditProfile_WithNoSelection_DoesNothing()
    {
        var vm = CreateViewModel();
        vm.SelectedProfile = null;

        vm.EditProfileCommand.Execute(null);

        vm.IsEditing.Should().BeFalse();
    }

    [Fact]
    public void EditProfile_WithBuiltInProfile_SetsError()
    {
        var vm = CreateViewModel();
        vm.SelectedProfile = new ProfileItemViewModel { Id = "builtin", IsBuiltIn = true };

        vm.EditProfileCommand.Execute(null);

        vm.ErrorMessage.Should().Contain("Built-in");
        vm.IsEditing.Should().BeFalse();
    }

    [Fact]
    public void EditProfile_EntersEditingMode()
    {
        var vm = CreateViewModel();
        vm.SelectedProfile = new ProfileItemViewModel
        {
            Id = "user",
            Name = "My Profile",
            Description = "Description",
            IsBuiltIn = false
        };

        vm.EditProfileCommand.Execute(null);

        vm.IsEditing.Should().BeTrue();
        vm.EditName.Should().Be("My Profile");
        vm.EditDescription.Should().Be("Description");
    }

    #endregion

    #region DeleteProfileAsync Tests

    [Fact]
    public async Task DeleteProfileAsync_WithBuiltInProfile_SetsError()
    {
        var vm = CreateViewModel();
        vm.SelectedProfile = new ProfileItemViewModel { Id = "builtin", IsBuiltIn = true };

        await vm.DeleteProfileCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Contain("Built-in");
    }

    [Fact]
    public async Task DeleteProfileAsync_WhenConfirmed_DeletesProfile()
    {
        _dialogServiceMock.Setup(s => s.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var profile = new ProfileItemViewModel { Id = "user", Name = "My Profile", IsBuiltIn = false };
        var vm = CreateViewModel();
        vm.Profiles.Add(profile);
        vm.SelectedProfile = profile;

        await vm.DeleteProfileCommand.ExecuteAsync(null);

        _profileServiceMock.Verify(s => s.DeleteProfileAsync("user", It.IsAny<CancellationToken>()), Times.Once);
        vm.Profiles.Should().BeEmpty();
        vm.SelectedProfile.Should().BeNull();
    }

    #endregion

    #region DuplicateProfileAsync Tests

    [Fact]
    public async Task DuplicateProfileAsync_CreatesDuplicate()
    {
        var duplicate = new ConfigurationProfile { Id = "dup", Name = "My Profile (Copy)" };
        _profileServiceMock.Setup(s => s.DuplicateProfileAsync("original", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(duplicate);

        var vm = CreateViewModel();
        vm.SelectedProfile = new ProfileItemViewModel { Id = "original", Name = "My Profile" };

        await vm.DuplicateProfileCommand.ExecuteAsync(null);

        vm.Profiles.Should().ContainSingle(p => p.Name == "My Profile (Copy)");
        vm.SuccessMessage.Should().Contain("duplicated");
    }

    #endregion

    #region SwitchToProfileAsync Tests

    [Fact]
    public async Task SwitchToProfileAsync_WhenConfirmed_SwitchesProfile()
    {
        _dialogServiceMock.Setup(s => s.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _profileServiceMock.Setup(s => s.SwitchToProfileAsync("selected", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfileSwitchResult { Success = true });

        var profile = new ProfileItemViewModel { Id = "selected", Name = "Selected Profile" };
        var vm = CreateViewModel();
        vm.Profiles.Add(profile);
        vm.SelectedProfile = profile;

        await vm.SwitchToProfileCommand.ExecuteAsync(null);

        vm.ActiveProfileId.Should().Be("selected");
        profile.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SwitchToProfileAsync_OffersToRestartWsl()
    {
        _dialogServiceMock.SetupSequence(s => s.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true)  // Confirm switch
            .ReturnsAsync(true); // Confirm restart
        _profileServiceMock.Setup(s => s.SwitchToProfileAsync("selected", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfileSwitchResult { Success = true });

        var vm = CreateViewModel();
        vm.SelectedProfile = new ProfileItemViewModel { Id = "selected", Name = "Selected Profile" };

        await vm.SwitchToProfileCommand.ExecuteAsync(null);

        _wslServiceMock.Verify(s => s.ShutdownAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Compare Tests

    [Fact]
    public void StartCompare_EntersCompareMode()
    {
        var vm = CreateViewModel();
        vm.SelectedProfile = new ProfileItemViewModel { Id = "1" };

        vm.StartCompareCommand.Execute(null);

        vm.IsComparing.Should().BeTrue();
        vm.IsEditing.Should().BeFalse();
    }

    [Fact]
    public async Task CompareWithAsync_ShowsDifferences()
    {
        var differences = new List<ProfileDifference>
        {
            new() { Section = "wsl2", Setting = "memory", Value1 = "8GB", Value2 = "16GB" }
        };
        _profileServiceMock.Setup(s => s.CompareProfilesAsync("1", "2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(differences);

        var vm = CreateViewModel();
        vm.SelectedProfile = new ProfileItemViewModel { Id = "1" };
        vm.CompareProfile = new ProfileItemViewModel { Id = "2" };

        await vm.CompareWithCommand.ExecuteAsync(null);

        vm.Differences.Should().HaveCount(1);
        vm.Differences[0].Setting.Should().Be("memory");
    }

    [Fact]
    public void ExitCompare_ExitsCompareMode()
    {
        var vm = CreateViewModel();
        vm.IsComparing = true;
        vm.Differences.Add(new ProfileDifferenceViewModel());

        vm.ExitCompareCommand.Execute(null);

        vm.IsComparing.Should().BeFalse();
        vm.Differences.Should().BeEmpty();
    }

    #endregion

    #region Import/Export Tests

    [Fact]
    public async Task ExportProfileAsync_ExportsToFile()
    {
        _dialogServiceMock.Setup(s => s.ShowSaveFileDialogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(@"C:\test\profile.json");

        var vm = CreateViewModel();
        vm.SelectedProfile = new ProfileItemViewModel { Id = "1", Name = "My Profile" };

        await vm.ExportProfileCommand.ExecuteAsync(null);

        _profileServiceMock.Verify(s => s.ExportProfileAsync("1", @"C:\test\profile.json", It.IsAny<CancellationToken>()), Times.Once);
        vm.SuccessMessage.Should().Contain("exported");
    }

    [Fact]
    public async Task ImportProfileAsync_ImportsFromFile()
    {
        _dialogServiceMock.Setup(s => s.ShowOpenFileDialogAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(@"C:\test\profile.json");
        _profileServiceMock.Setup(s => s.ImportProfileAsync(@"C:\test\profile.json", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConfigurationProfile { Id = "imported", Name = "Imported Profile" });

        var vm = CreateViewModel();

        await vm.ImportProfileCommand.ExecuteAsync(null);

        vm.Profiles.Should().ContainSingle(p => p.Name == "Imported Profile");
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
}
