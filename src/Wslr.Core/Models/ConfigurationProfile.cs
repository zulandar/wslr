namespace Wslr.Core.Models;

/// <summary>
/// Represents a named configuration profile for global WSL settings.
/// Profiles capture a complete .wslconfig state that can be quickly switched.
/// </summary>
public sealed record ConfigurationProfile
{
    /// <summary>
    /// Gets the unique identifier for the profile.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Gets the display name of the profile.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets a description of what this profile is for.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is a built-in profile (read-only).
    /// </summary>
    public bool IsBuiltIn { get; init; }

    /// <summary>
    /// Gets the date the profile was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the date the profile was last modified.
    /// </summary>
    public DateTime ModifiedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the global .wslconfig settings for this profile.
    /// </summary>
    public WslConfig Settings { get; init; } = new();
}

/// <summary>
/// Result of switching to a profile.
/// </summary>
public sealed record ProfileSwitchResult
{
    /// <summary>
    /// Gets a value indicating whether the switch succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the error message if the switch failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets a value indicating whether WSL restart is required.
    /// </summary>
    public bool RestartRequired { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ProfileSwitchResult Succeeded() => new()
    {
        Success = true,
        RestartRequired = true
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static ProfileSwitchResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Represents a difference between two profile settings.
/// </summary>
public sealed record ProfileDifference
{
    /// <summary>
    /// Gets the section name.
    /// </summary>
    public string Section { get; init; } = string.Empty;

    /// <summary>
    /// Gets the setting name.
    /// </summary>
    public string Setting { get; init; } = string.Empty;

    /// <summary>
    /// Gets the value in the first profile.
    /// </summary>
    public string? Value1 { get; init; }

    /// <summary>
    /// Gets the value in the second profile.
    /// </summary>
    public string? Value2 { get; init; }
}
