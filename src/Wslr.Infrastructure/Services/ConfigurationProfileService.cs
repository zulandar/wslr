using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Wslr.Core.Interfaces;
using Wslr.Core.Models;

namespace Wslr.Infrastructure.Services;

/// <summary>
/// Service for managing WSL configuration profiles.
/// </summary>
public sealed class ConfigurationProfileService : IConfigurationProfileService
{
    private readonly IWslConfigService _wslConfigService;
    private readonly ILogger<ConfigurationProfileService> _logger;
    private readonly string _profilesPath;
    private readonly string _activeProfilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly List<ConfigurationProfile> _builtInProfiles;
    private string? _activeProfileId;

    /// <inheritdoc />
    public event EventHandler<string?>? ActiveProfileChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationProfileService"/> class.
    /// </summary>
    public ConfigurationProfileService(
        IWslConfigService wslConfigService,
        ILogger<ConfigurationProfileService> logger)
    {
        _wslConfigService = wslConfigService ?? throw new ArgumentNullException(nameof(wslConfigService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _profilesPath = Path.Combine(appDataPath, "WSLR", "Profiles");
        _activeProfilePath = Path.Combine(appDataPath, "WSLR", "active-profile.txt");
        Directory.CreateDirectory(_profilesPath);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        _builtInProfiles = CreateBuiltInProfiles();
        LoadActiveProfileId();
    }

    /// <inheritdoc />
    public IReadOnlyList<ConfigurationProfile> GetBuiltInProfiles() => _builtInProfiles;

    /// <inheritdoc />
    public string? GetActiveProfileId() => _activeProfileId;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigurationProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default)
    {
        var profiles = new List<ConfigurationProfile>(_builtInProfiles);

        if (Directory.Exists(_profilesPath))
        {
            foreach (var file in Directory.GetFiles(_profilesPath, "*.json"))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file, cancellationToken);
                    var profile = JsonSerializer.Deserialize<ConfigurationProfile>(json, _jsonOptions);
                    if (profile is not null)
                    {
                        profiles.Add(profile);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load profile from {File}", file);
                }
            }
        }

        return profiles.OrderBy(p => p.IsBuiltIn ? 0 : 1).ThenBy(p => p.Name).ToList();
    }

    /// <inheritdoc />
    public async Task<ConfigurationProfile?> GetProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        var builtIn = _builtInProfiles.FirstOrDefault(p => p.Id == profileId);
        if (builtIn is not null)
        {
            return builtIn;
        }

        var filePath = GetProfileFilePath(profileId);
        if (File.Exists(filePath))
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            return JsonSerializer.Deserialize<ConfigurationProfile>(json, _jsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<ConfigurationProfile> CreateProfileAsync(ConfigurationProfile profile, CancellationToken cancellationToken = default)
    {
        if (profile.IsBuiltIn)
        {
            throw new InvalidOperationException("Cannot create a built-in profile.");
        }

        var newProfile = profile with
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            IsBuiltIn = false
        };

        await SaveProfileAsync(newProfile, cancellationToken);
        _logger.LogInformation("Created profile: {Name} ({Id})", newProfile.Name, newProfile.Id);

        return newProfile;
    }

    /// <inheritdoc />
    public async Task<ConfigurationProfile> CreateProfileFromCurrentAsync(
        string name,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var currentSettings = await _wslConfigService.ReadConfigAsync(cancellationToken);

        var profile = new ConfigurationProfile
        {
            Name = name,
            Description = description ?? "Created from current settings",
            Settings = currentSettings
        };

        return await CreateProfileAsync(profile, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConfigurationProfile> UpdateProfileAsync(ConfigurationProfile profile, CancellationToken cancellationToken = default)
    {
        if (profile.IsBuiltIn || _builtInProfiles.Any(p => p.Id == profile.Id))
        {
            throw new InvalidOperationException("Cannot modify a built-in profile.");
        }

        var updatedProfile = profile with
        {
            ModifiedAt = DateTime.UtcNow
        };

        await SaveProfileAsync(updatedProfile, cancellationToken);
        _logger.LogInformation("Updated profile: {Name} ({Id})", updatedProfile.Name, updatedProfile.Id);

        return updatedProfile;
    }

    /// <inheritdoc />
    public Task<bool> DeleteProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        if (_builtInProfiles.Any(p => p.Id == profileId))
        {
            throw new InvalidOperationException("Cannot delete a built-in profile.");
        }

        var filePath = GetProfileFilePath(profileId);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("Deleted profile: {Id}", profileId);

            // Clear active profile if it was deleted
            if (_activeProfileId == profileId)
            {
                SetActiveProfileId(null);
            }

            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public async Task<ConfigurationProfile> DuplicateProfileAsync(string profileId, string? newName = null, CancellationToken cancellationToken = default)
    {
        var original = await GetProfileAsync(profileId, cancellationToken)
            ?? throw new InvalidOperationException($"Profile '{profileId}' not found.");

        var duplicate = original with
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            Name = newName ?? $"{original.Name} (Copy)",
            IsBuiltIn = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        await SaveProfileAsync(duplicate, cancellationToken);
        _logger.LogInformation("Duplicated profile: {OriginalId} -> {NewId}", profileId, duplicate.Id);

        return duplicate;
    }

    /// <inheritdoc />
    public async Task<ProfileSwitchResult> SwitchToProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        var profile = await GetProfileAsync(profileId, cancellationToken);
        if (profile is null)
        {
            return ProfileSwitchResult.Failed($"Profile '{profileId}' not found.");
        }

        try
        {
            // Create backup before switching
            await _wslConfigService.CreateBackupAsync(cancellationToken);

            // Write the profile settings to .wslconfig
            await _wslConfigService.WriteConfigAsync(profile.Settings, cancellationToken);

            // Update active profile
            SetActiveProfileId(profileId);

            _logger.LogInformation("Switched to profile: {Name} ({Id})", profile.Name, profileId);
            return ProfileSwitchResult.Succeeded();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch to profile {ProfileId}", profileId);
            return ProfileSwitchResult.Failed(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProfileDifference>> CompareProfilesAsync(
        string profileId1,
        string profileId2,
        CancellationToken cancellationToken = default)
    {
        var profile1 = await GetProfileAsync(profileId1, cancellationToken)
            ?? throw new InvalidOperationException($"Profile '{profileId1}' not found.");
        var profile2 = await GetProfileAsync(profileId2, cancellationToken)
            ?? throw new InvalidOperationException($"Profile '{profileId2}' not found.");

        var differences = new List<ProfileDifference>();

        // Compare WSL2 settings
        CompareValue(differences, "wsl2", "memory", profile1.Settings.Wsl2.Memory, profile2.Settings.Wsl2.Memory);
        CompareValue(differences, "wsl2", "processors", profile1.Settings.Wsl2.Processors?.ToString(), profile2.Settings.Wsl2.Processors?.ToString());
        CompareValue(differences, "wsl2", "swap", profile1.Settings.Wsl2.Swap, profile2.Settings.Wsl2.Swap);
        CompareValue(differences, "wsl2", "localhostForwarding", profile1.Settings.Wsl2.LocalhostForwarding?.ToString(), profile2.Settings.Wsl2.LocalhostForwarding?.ToString());
        CompareValue(differences, "wsl2", "guiApplications", profile1.Settings.Wsl2.GuiApplications?.ToString(), profile2.Settings.Wsl2.GuiApplications?.ToString());
        CompareValue(differences, "wsl2", "nestedVirtualization", profile1.Settings.Wsl2.NestedVirtualization?.ToString(), profile2.Settings.Wsl2.NestedVirtualization?.ToString());
        CompareValue(differences, "wsl2", "networkingMode", profile1.Settings.Wsl2.NetworkingMode, profile2.Settings.Wsl2.NetworkingMode);
        CompareValue(differences, "wsl2", "pageReporting", profile1.Settings.Wsl2.PageReporting?.ToString(), profile2.Settings.Wsl2.PageReporting?.ToString());
        CompareValue(differences, "wsl2", "dnsTunneling", profile1.Settings.Wsl2.DnsTunneling?.ToString(), profile2.Settings.Wsl2.DnsTunneling?.ToString());
        CompareValue(differences, "wsl2", "firewall", profile1.Settings.Wsl2.Firewall?.ToString(), profile2.Settings.Wsl2.Firewall?.ToString());

        // Compare Experimental settings
        CompareValue(differences, "experimental", "autoMemoryReclaim", profile1.Settings.Experimental.AutoMemoryReclaim, profile2.Settings.Experimental.AutoMemoryReclaim);
        CompareValue(differences, "experimental", "sparseVhd", profile1.Settings.Experimental.SparseVhd?.ToString(), profile2.Settings.Experimental.SparseVhd?.ToString());

        return differences;
    }

    /// <inheritdoc />
    public async Task ExportProfileAsync(string profileId, string filePath, CancellationToken cancellationToken = default)
    {
        var profile = await GetProfileAsync(profileId, cancellationToken)
            ?? throw new InvalidOperationException($"Profile '{profileId}' not found.");

        var exportProfile = profile with { IsBuiltIn = false };
        var json = JsonSerializer.Serialize(exportProfile, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
        _logger.LogInformation("Exported profile {ProfileId} to {Path}", profileId, filePath);
    }

    /// <inheritdoc />
    public async Task<ConfigurationProfile> ImportProfileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var profile = JsonSerializer.Deserialize<ConfigurationProfile>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to parse profile file.");

        var importedProfile = profile with
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            IsBuiltIn = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        await SaveProfileAsync(importedProfile, cancellationToken);
        _logger.LogInformation("Imported profile from {Path} as {Id}", filePath, importedProfile.Id);

        return importedProfile;
    }

    private string GetProfileFilePath(string profileId) =>
        Path.Combine(_profilesPath, $"{profileId}.json");

    private async Task SaveProfileAsync(ConfigurationProfile profile, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(profile, _jsonOptions);
        var filePath = GetProfileFilePath(profile.Id);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    private void LoadActiveProfileId()
    {
        if (File.Exists(_activeProfilePath))
        {
            try
            {
                _activeProfileId = File.ReadAllText(_activeProfilePath).Trim();
                if (string.IsNullOrEmpty(_activeProfileId))
                {
                    _activeProfileId = null;
                }
            }
            catch
            {
                _activeProfileId = null;
            }
        }
    }

    private void SetActiveProfileId(string? profileId)
    {
        _activeProfileId = profileId;
        try
        {
            if (profileId is null)
            {
                if (File.Exists(_activeProfilePath))
                {
                    File.Delete(_activeProfilePath);
                }
            }
            else
            {
                File.WriteAllText(_activeProfilePath, profileId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist active profile ID");
        }

        ActiveProfileChanged?.Invoke(this, profileId);
    }

    private static void CompareValue(List<ProfileDifference> differences, string section, string setting, string? value1, string? value2)
    {
        if (!string.Equals(value1, value2, StringComparison.OrdinalIgnoreCase))
        {
            differences.Add(new ProfileDifference
            {
                Section = section,
                Setting = setting,
                Value1 = value1,
                Value2 = value2
            });
        }
    }

    private static List<ConfigurationProfile> CreateBuiltInProfiles()
    {
        return
        [
            new ConfigurationProfile
            {
                Id = "profile-balanced",
                Name = "Balanced",
                Description = "Default settings - balanced performance for general use",
                IsBuiltIn = true,
                Settings = new WslConfig
                {
                    Wsl2 = new Wsl2Settings
                    {
                        GuiApplications = true,
                        NestedVirtualization = true,
                        LocalhostForwarding = true,
                        PageReporting = true
                    }
                }
            },
            new ConfigurationProfile
            {
                Id = "profile-performance",
                Name = "High Performance",
                Description = "Maximum resources for demanding workloads (Docker, builds)",
                IsBuiltIn = true,
                Settings = new WslConfig
                {
                    Wsl2 = new Wsl2Settings
                    {
                        Memory = "16GB",
                        Processors = Environment.ProcessorCount,
                        Swap = "8GB",
                        GuiApplications = true,
                        NestedVirtualization = true,
                        LocalhostForwarding = true,
                        PageReporting = true
                    },
                    Experimental = new ExperimentalSettings
                    {
                        SparseVhd = true
                    }
                }
            },
            new ConfigurationProfile
            {
                Id = "profile-lowmem",
                Name = "Low Memory",
                Description = "Minimal resource usage for constrained systems",
                IsBuiltIn = true,
                Settings = new WslConfig
                {
                    Wsl2 = new Wsl2Settings
                    {
                        Memory = "4GB",
                        Processors = 2,
                        Swap = "2GB",
                        GuiApplications = false,
                        NestedVirtualization = false,
                        PageReporting = true
                    },
                    Experimental = new ExperimentalSettings
                    {
                        AutoMemoryReclaim = "gradual",
                        SparseVhd = true
                    }
                }
            },
            new ConfigurationProfile
            {
                Id = "profile-gaming",
                Name = "Gaming Mode",
                Description = "Minimal WSL resources to maximize host performance",
                IsBuiltIn = true,
                Settings = new WslConfig
                {
                    Wsl2 = new Wsl2Settings
                    {
                        Memory = "2GB",
                        Processors = 2,
                        Swap = "1GB",
                        GuiApplications = false,
                        NestedVirtualization = false,
                        PageReporting = true
                    },
                    Experimental = new ExperimentalSettings
                    {
                        AutoMemoryReclaim = "dropcache",
                        SparseVhd = true
                    }
                }
            }
        ];
    }
}
