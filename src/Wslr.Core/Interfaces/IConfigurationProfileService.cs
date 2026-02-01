using Wslr.Core.Models;

namespace Wslr.Core.Interfaces;

/// <summary>
/// Provides operations for managing WSL configuration profiles.
/// Profiles capture global .wslconfig settings for quick switching.
/// </summary>
public interface IConfigurationProfileService
{
    /// <summary>
    /// Event raised when the active profile changes.
    /// </summary>
    event EventHandler<string?>? ActiveProfileChanged;

    /// <summary>
    /// Gets all available profiles (built-in and user).
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of all profiles.</returns>
    Task<IReadOnlyList<ConfigurationProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a profile by its ID.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The profile, or null if not found.</returns>
    Task<ConfigurationProfile?> GetProfileAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all built-in profiles.
    /// </summary>
    /// <returns>A list of built-in profiles.</returns>
    IReadOnlyList<ConfigurationProfile> GetBuiltInProfiles();

    /// <summary>
    /// Gets the currently active profile ID (if any).
    /// </summary>
    /// <returns>The active profile ID, or null if no profile is active.</returns>
    string? GetActiveProfileId();

    /// <summary>
    /// Creates a new profile.
    /// </summary>
    /// <param name="profile">The profile to create.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The created profile with assigned ID.</returns>
    Task<ConfigurationProfile> CreateProfileAsync(ConfigurationProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a profile from the current .wslconfig settings.
    /// </summary>
    /// <param name="name">The profile name.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The created profile.</returns>
    Task<ConfigurationProfile> CreateProfileFromCurrentAsync(
        string name,
        string? description = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing profile.
    /// </summary>
    /// <param name="profile">The profile with updated values.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The updated profile.</returns>
    Task<ConfigurationProfile> UpdateProfileAsync(ConfigurationProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a profile.
    /// </summary>
    /// <param name="profileId">The ID of the profile to delete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the profile was deleted.</returns>
    Task<bool> DeleteProfileAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Duplicates a profile.
    /// </summary>
    /// <param name="profileId">The ID of the profile to duplicate.</param>
    /// <param name="newName">Optional new name for the duplicate.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The duplicated profile.</returns>
    Task<ConfigurationProfile> DuplicateProfileAsync(string profileId, string? newName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Switches to a profile by applying its settings to .wslconfig.
    /// </summary>
    /// <param name="profileId">The ID of the profile to switch to.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of the switch operation.</returns>
    Task<ProfileSwitchResult> SwitchToProfileAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares two profiles and returns the differences.
    /// </summary>
    /// <param name="profileId1">The first profile ID.</param>
    /// <param name="profileId2">The second profile ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of differences between the profiles.</returns>
    Task<IReadOnlyList<ProfileDifference>> CompareProfilesAsync(
        string profileId1,
        string profileId2,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a profile to a JSON file.
    /// </summary>
    /// <param name="profileId">The ID of the profile to export.</param>
    /// <param name="filePath">The path to export to.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task ExportProfileAsync(string profileId, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a profile from a JSON file.
    /// </summary>
    /// <param name="filePath">The path to import from.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The imported profile.</returns>
    Task<ConfigurationProfile> ImportProfileAsync(string filePath, CancellationToken cancellationToken = default);
}
