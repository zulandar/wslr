namespace Wslr.Core.Models;

/// <summary>
/// Represents the complete .wslconfig configuration file.
/// </summary>
public sealed record WslConfig
{
    /// <summary>
    /// Gets the [wsl2] section settings.
    /// </summary>
    public Wsl2Settings Wsl2 { get; init; } = new();

    /// <summary>
    /// Gets the [experimental] section settings.
    /// </summary>
    public ExperimentalSettings Experimental { get; init; } = new();

    /// <summary>
    /// Gets additional sections not explicitly defined.
    /// Key is section name, value is dictionary of key-value pairs.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> AdditionalSections { get; init; } =
        new Dictionary<string, IReadOnlyDictionary<string, string>>();
}
