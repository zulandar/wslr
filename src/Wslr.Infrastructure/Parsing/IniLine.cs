namespace Wslr.Infrastructure.Parsing;

/// <summary>
/// Represents the type of a line in an INI file.
/// </summary>
public enum IniLineType
{
    /// <summary>An empty or whitespace-only line.</summary>
    Empty,

    /// <summary>A comment line starting with ; or #.</summary>
    Comment,

    /// <summary>A section header like [section].</summary>
    Section,

    /// <summary>A key-value pair like key=value.</summary>
    KeyValue
}

/// <summary>
/// Represents a single line in an INI document.
/// </summary>
public sealed record IniLine
{
    /// <summary>
    /// Gets the type of this line.
    /// </summary>
    public required IniLineType Type { get; init; }

    /// <summary>
    /// Gets the original raw text of the line (for preservation).
    /// </summary>
    public required string RawText { get; init; }

    /// <summary>
    /// Gets the section name (for Section lines).
    /// </summary>
    public string? SectionName { get; init; }

    /// <summary>
    /// Gets the key (for KeyValue lines).
    /// </summary>
    public string? Key { get; init; }

    /// <summary>
    /// Gets the value (for KeyValue lines).
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    /// Gets the inline comment (text after # or ; on a key-value line).
    /// </summary>
    public string? InlineComment { get; init; }

    /// <summary>
    /// Creates an empty line.
    /// </summary>
    /// <param name="rawText">The original line text.</param>
    /// <returns>An empty line.</returns>
    public static IniLine Empty(string rawText = "") => new()
    {
        Type = IniLineType.Empty,
        RawText = rawText
    };

    /// <summary>
    /// Creates a comment line.
    /// </summary>
    /// <param name="rawText">The original line text including the comment marker.</param>
    /// <returns>A comment line.</returns>
    public static IniLine Comment(string rawText) => new()
    {
        Type = IniLineType.Comment,
        RawText = rawText
    };

    /// <summary>
    /// Creates a section header line.
    /// </summary>
    /// <param name="sectionName">The section name.</param>
    /// <param name="rawText">The original line text.</param>
    /// <returns>A section line.</returns>
    public static IniLine Section(string sectionName, string rawText) => new()
    {
        Type = IniLineType.Section,
        RawText = rawText,
        SectionName = sectionName
    };

    /// <summary>
    /// Creates a key-value line.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="rawText">The original line text.</param>
    /// <param name="inlineComment">Optional inline comment.</param>
    /// <returns>A key-value line.</returns>
    public static IniLine KeyValue(string key, string value, string rawText, string? inlineComment = null) => new()
    {
        Type = IniLineType.KeyValue,
        RawText = rawText,
        Key = key,
        Value = value,
        InlineComment = inlineComment
    };

    /// <summary>
    /// Creates a new key-value line with an updated value.
    /// </summary>
    /// <param name="newValue">The new value.</param>
    /// <returns>A new line with the updated value.</returns>
    public IniLine WithValue(string newValue)
    {
        if (Type != IniLineType.KeyValue || Key is null)
        {
            throw new InvalidOperationException("Cannot set value on a non-key-value line.");
        }

        // Reconstruct the raw text with the new value
        var newRawText = InlineComment is not null
            ? $"{Key}={newValue} {InlineComment}"
            : $"{Key}={newValue}";

        return this with
        {
            Value = newValue,
            RawText = newRawText
        };
    }
}
