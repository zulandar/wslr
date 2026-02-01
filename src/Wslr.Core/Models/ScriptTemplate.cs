namespace Wslr.Core.Models;

/// <summary>
/// A reusable setup script template that can be applied to WSL distributions.
/// </summary>
public sealed record ScriptTemplate
{
    /// <summary>
    /// Unique identifier for the template.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Display name for the template.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Optional description of what the script does.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The bash script content.
    /// </summary>
    public required string ScriptContent { get; init; }

    /// <summary>
    /// Optional category for organizing templates.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Whether this is a built-in template (read-only).
    /// </summary>
    public bool IsBuiltIn { get; init; }

    /// <summary>
    /// Template variables that can be substituted at execution time.
    /// Key is the variable name (e.g., "USERNAME"), value is the default value.
    /// </summary>
    public Dictionary<string, string>? Variables { get; init; }

    /// <summary>
    /// When the template was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When the template was last modified.
    /// </summary>
    public DateTime ModifiedAt { get; init; } = DateTime.UtcNow;
}
