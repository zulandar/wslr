using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Wslr.Core.Interfaces;
using Wslr.Core.Models;
using Wslr.UI.Services;

namespace Wslr.UI.ViewModels;

/// <summary>
/// ViewModel for the profile list and management view.
/// </summary>
public partial class ProfileListViewModel : ObservableObject
{
    private readonly IConfigurationProfileService _profileService;
    private readonly IWslService _wslService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<ProfileListViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<ProfileItemViewModel> _profiles = [];

    [ObservableProperty]
    private ProfileItemViewModel? _selectedProfile;

    [ObservableProperty]
    private ProfileItemViewModel? _compareProfile;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _successMessage;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string _editDescription = string.Empty;

    [ObservableProperty]
    private bool _isComparing;

    [ObservableProperty]
    private ObservableCollection<ProfileDifferenceViewModel> _differences = [];

    [ObservableProperty]
    private string? _activeProfileId;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileListViewModel"/> class.
    /// </summary>
    public ProfileListViewModel(
        IConfigurationProfileService profileService,
        IWslService wslService,
        IDialogService dialogService,
        ILogger<ProfileListViewModel> logger)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _wslService = wslService ?? throw new ArgumentNullException(nameof(wslService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _profileService.ActiveProfileChanged += OnActiveProfileChanged;
    }

    private void OnActiveProfileChanged(object? sender, string? profileId)
    {
        ActiveProfileId = profileId;
        foreach (var profile in Profiles)
        {
            profile.IsActive = profile.Id == profileId;
        }
    }

    /// <summary>
    /// Loads all profiles.
    /// </summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            ActiveProfileId = _profileService.GetActiveProfileId();
            var profiles = await _profileService.GetAllProfilesAsync();
            Profiles.Clear();
            foreach (var profile in profiles)
            {
                var vm = ProfileItemViewModel.FromModel(profile);
                vm.IsActive = profile.Id == ActiveProfileId;
                Profiles.Add(vm);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load profiles");
            ErrorMessage = $"Failed to load profiles: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Creates a new profile.
    /// </summary>
    [RelayCommand]
    public void NewProfile()
    {
        IsEditing = true;
        IsComparing = false;
        EditName = "New Profile";
        EditDescription = string.Empty;
        SelectedProfile = null;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    /// <summary>
    /// Creates a profile from current .wslconfig settings.
    /// </summary>
    [RelayCommand]
    public async Task CreateFromCurrentAsync()
    {
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Create Profile from Current Settings",
            "Create a new profile from your current .wslconfig settings?");

        if (!confirmed)
        {
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            var profile = await _profileService.CreateProfileFromCurrentAsync(
                "My Profile",
                "Created from current settings");

            var vm = ProfileItemViewModel.FromModel(profile);
            Profiles.Add(vm);
            SelectedProfile = vm;
            SuccessMessage = "Profile created from current settings.";
            _logger.LogInformation("Created profile from current settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create profile from current settings");
            ErrorMessage = $"Failed to create profile: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// Saves the currently edited profile.
    /// </summary>
    [RelayCommand]
    public async Task SaveProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            ErrorMessage = "Profile name is required.";
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            ConfigurationProfile profile;
            if (SelectedProfile is not null && !SelectedProfile.IsBuiltIn)
            {
                var existing = await _profileService.GetProfileAsync(SelectedProfile.Id);
                if (existing is not null)
                {
                    profile = await _profileService.UpdateProfileAsync(existing with
                    {
                        Name = EditName,
                        Description = EditDescription
                    });

                    SelectedProfile.Name = profile.Name;
                    SelectedProfile.Description = profile.Description;
                }
            }
            else
            {
                // Create new profile with default settings
                profile = await _profileService.CreateProfileAsync(new ConfigurationProfile
                {
                    Name = EditName,
                    Description = EditDescription,
                    Settings = new WslConfig()
                });

                var vm = ProfileItemViewModel.FromModel(profile);
                Profiles.Add(vm);
            }

            IsEditing = false;
            SuccessMessage = "Profile saved.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save profile");
            ErrorMessage = $"Failed to save profile: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// Cancels editing.
    /// </summary>
    [RelayCommand]
    public void CancelEdit()
    {
        IsEditing = false;
        EditName = string.Empty;
        EditDescription = string.Empty;
    }

    /// <summary>
    /// Edits the selected profile.
    /// </summary>
    [RelayCommand]
    public void EditProfile()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        if (SelectedProfile.IsBuiltIn)
        {
            ErrorMessage = "Built-in profiles cannot be edited. Duplicate it first.";
            return;
        }

        IsEditing = true;
        IsComparing = false;
        EditName = SelectedProfile.Name;
        EditDescription = SelectedProfile.Description ?? string.Empty;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    /// <summary>
    /// Duplicates the selected profile.
    /// </summary>
    [RelayCommand]
    public async Task DuplicateProfileAsync()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            var duplicate = await _profileService.DuplicateProfileAsync(SelectedProfile.Id);
            var vm = ProfileItemViewModel.FromModel(duplicate);
            Profiles.Add(vm);
            SuccessMessage = $"Profile duplicated as '{duplicate.Name}'";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to duplicate profile");
            ErrorMessage = $"Failed to duplicate profile: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// Deletes the selected profile.
    /// </summary>
    [RelayCommand]
    public async Task DeleteProfileAsync()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        if (SelectedProfile.IsBuiltIn)
        {
            ErrorMessage = "Built-in profiles cannot be deleted.";
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Profile",
            $"Are you sure you want to delete the profile '{SelectedProfile.Name}'?");

        if (!confirmed)
        {
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            await _profileService.DeleteProfileAsync(SelectedProfile.Id);
            Profiles.Remove(SelectedProfile);
            SelectedProfile = null;
            SuccessMessage = "Profile deleted.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete profile");
            ErrorMessage = $"Failed to delete profile: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// Switches to the selected profile.
    /// </summary>
    [RelayCommand]
    public async Task SwitchToProfileAsync()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Switch Profile",
            $"Switch to profile '{SelectedProfile.Name}'?\n\n" +
            "This will update your global WSL settings (.wslconfig).\n" +
            "You will need to restart WSL for changes to take effect.");

        if (!confirmed)
        {
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            var result = await _profileService.SwitchToProfileAsync(SelectedProfile.Id);

            if (result.Success)
            {
                // Update UI to show new active profile
                foreach (var p in Profiles)
                {
                    p.IsActive = p.Id == SelectedProfile.Id;
                }
                ActiveProfileId = SelectedProfile.Id;

                // Offer to restart WSL
                var restart = await _dialogService.ShowConfirmationAsync(
                    "Restart WSL",
                    "Profile applied. Restart WSL now for changes to take effect?\n\n" +
                    "This will shut down all running distributions.");

                if (restart)
                {
                    await _wslService.ShutdownAsync();
                    SuccessMessage = $"Switched to '{SelectedProfile.Name}' and restarted WSL.";
                }
                else
                {
                    SuccessMessage = $"Switched to '{SelectedProfile.Name}'. Restart WSL when ready.";
                }
            }
            else
            {
                ErrorMessage = $"Failed to switch profile: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch profile");
            ErrorMessage = $"Failed to switch profile: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// Starts profile comparison mode.
    /// </summary>
    [RelayCommand]
    public void StartCompare()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        IsComparing = true;
        IsEditing = false;
        CompareProfile = null;
        Differences.Clear();
        SuccessMessage = null;
        ErrorMessage = null;
    }

    /// <summary>
    /// Compares the selected profile with another.
    /// </summary>
    [RelayCommand]
    public async Task CompareWithAsync()
    {
        if (SelectedProfile is null || CompareProfile is null)
        {
            return;
        }

        if (SelectedProfile.Id == CompareProfile.Id)
        {
            ErrorMessage = "Cannot compare a profile with itself.";
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            var diffs = await _profileService.CompareProfilesAsync(SelectedProfile.Id, CompareProfile.Id);
            Differences.Clear();
            foreach (var diff in diffs)
            {
                Differences.Add(new ProfileDifferenceViewModel
                {
                    Section = diff.Section,
                    Setting = diff.Setting,
                    Value1 = diff.Value1 ?? "(not set)",
                    Value2 = diff.Value2 ?? "(not set)"
                });
            }

            if (Differences.Count == 0)
            {
                SuccessMessage = "Profiles are identical.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare profiles");
            ErrorMessage = $"Failed to compare: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// Exits comparison mode.
    /// </summary>
    [RelayCommand]
    public void ExitCompare()
    {
        IsComparing = false;
        CompareProfile = null;
        Differences.Clear();
    }

    /// <summary>
    /// Exports the selected profile.
    /// </summary>
    [RelayCommand]
    public async Task ExportProfileAsync()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        var fileName = $"{SelectedProfile.Name.Replace(' ', '-')}.wslr-profile.json";
        var filePath = await _dialogService.ShowSaveFileDialogAsync(
            "Export Profile",
            fileName,
            "Profile files|*.wslr-profile.json|JSON files|*.json|All files|*.*");

        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            await _profileService.ExportProfileAsync(SelectedProfile.Id, filePath);
            SuccessMessage = $"Profile exported to {filePath}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export profile");
            ErrorMessage = $"Failed to export: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// Imports a profile.
    /// </summary>
    [RelayCommand]
    public async Task ImportProfileAsync()
    {
        var filePath = await _dialogService.ShowOpenFileDialogAsync(
            "Import Profile",
            "Profile files|*.wslr-profile.json|JSON files|*.json|All files|*.*");

        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            var profile = await _profileService.ImportProfileAsync(filePath);
            var vm = ProfileItemViewModel.FromModel(profile);
            Profiles.Add(vm);
            SuccessMessage = $"Profile '{profile.Name}' imported.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import profile");
            ErrorMessage = $"Failed to import: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    partial void OnSelectedProfileChanged(ProfileItemViewModel? value)
    {
        if (value is not null && IsEditing)
        {
            CancelEdit();
        }
        if (value is not null && IsComparing)
        {
            ExitCompare();
        }
    }
}

/// <summary>
/// ViewModel for a single profile item.
/// </summary>
public partial class ProfileItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private bool _isBuiltIn;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private DateTime _modifiedAt;

    /// <summary>
    /// Creates a view model from a profile model.
    /// </summary>
    public static ProfileItemViewModel FromModel(ConfigurationProfile profile)
    {
        return new ProfileItemViewModel
        {
            Id = profile.Id,
            Name = profile.Name,
            Description = profile.Description,
            IsBuiltIn = profile.IsBuiltIn,
            ModifiedAt = profile.ModifiedAt
        };
    }
}

/// <summary>
/// ViewModel for a profile difference.
/// </summary>
public partial class ProfileDifferenceViewModel : ObservableObject
{
    [ObservableProperty]
    private string _section = string.Empty;

    [ObservableProperty]
    private string _setting = string.Empty;

    [ObservableProperty]
    private string _value1 = string.Empty;

    [ObservableProperty]
    private string _value2 = string.Empty;
}
