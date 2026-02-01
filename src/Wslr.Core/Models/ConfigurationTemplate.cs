namespace Wslr.Core.Models;

/// <summary>
/// Represents a reusable WSL configuration template.
/// </summary>
public sealed record ConfigurationTemplate
{
    /// <summary>
    /// Gets the unique identifier for the template.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Gets the display name of the template.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets a description of what this template configures.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is a built-in template (read-only).
    /// </summary>
    public bool IsBuiltIn { get; init; }

    /// <summary>
    /// Gets the date the template was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the date the template was last modified.
    /// </summary>
    public DateTime ModifiedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the global .wslconfig settings (optional).
    /// </summary>
    public WslConfig? GlobalSettings { get; init; }

    /// <summary>
    /// Gets the per-distribution wsl.conf settings (optional).
    /// </summary>
    public WslDistroConfig? DistroSettings { get; init; }
}

/// <summary>
/// Result of applying a template to a distribution.
/// </summary>
public sealed record TemplateApplyResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets a value indicating whether the global settings were applied.
    /// </summary>
    public bool GlobalSettingsApplied { get; init; }

    /// <summary>
    /// Gets a value indicating whether the distribution settings were applied.
    /// </summary>
    public bool DistroSettingsApplied { get; init; }

    /// <summary>
    /// Gets a value indicating whether a restart is required.
    /// </summary>
    public bool RestartRequired { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static TemplateApplyResult Succeeded(bool globalApplied, bool distroApplied) => new()
    {
        Success = true,
        GlobalSettingsApplied = globalApplied,
        DistroSettingsApplied = distroApplied,
        RestartRequired = globalApplied || distroApplied
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static TemplateApplyResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Options for applying a template.
/// </summary>
public sealed record TemplateApplyOptions
{
    /// <summary>
    /// Gets a value indicating whether to apply global settings.
    /// </summary>
    public bool ApplyGlobalSettings { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to apply distribution settings.
    /// </summary>
    public bool ApplyDistroSettings { get; init; } = true;

    /// <summary>
    /// Gets the merge mode for applying settings.
    /// </summary>
    public TemplateMergeMode MergeMode { get; init; } = TemplateMergeMode.Merge;
}

/// <summary>
/// Specifies how template settings are merged with existing settings.
/// </summary>
public enum TemplateMergeMode
{
    /// <summary>
    /// Merge template settings with existing settings (template values take precedence).
    /// </summary>
    Merge,

    /// <summary>
    /// Replace all existing settings with template settings.
    /// </summary>
    Overwrite
}
