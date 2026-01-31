using System.Text;

namespace Wslr.Infrastructure.Parsing;

/// <summary>
/// Represents an INI document that preserves structure, comments, and formatting.
/// </summary>
public sealed class IniDocument
{
    private readonly List<IniLine> _lines = [];

    /// <summary>
    /// Gets all lines in the document.
    /// </summary>
    public IReadOnlyList<IniLine> Lines => _lines;

    /// <summary>
    /// Gets all section names in the document.
    /// </summary>
    public IEnumerable<string> Sections => _lines
        .Where(l => l.Type == IniLineType.Section && l.SectionName is not null)
        .Select(l => l.SectionName!);

    /// <summary>
    /// Parses an INI document from text.
    /// </summary>
    /// <param name="content">The INI file content.</param>
    /// <returns>A parsed INI document.</returns>
    public static IniDocument Parse(string content)
    {
        var document = new IniDocument();

        if (string.IsNullOrEmpty(content))
        {
            return document;
        }

        var lines = content.Split('\n');

        foreach (var rawLine in lines)
        {
            // Preserve original line ending style
            var line = rawLine.TrimEnd('\r');
            document._lines.Add(ParseLine(line));
        }

        return document;
    }

    private static IniLine ParseLine(string line)
    {
        var trimmed = line.Trim();

        // Empty line
        if (string.IsNullOrEmpty(trimmed))
        {
            return IniLine.Empty(line);
        }

        // Comment line
        if (trimmed.StartsWith(';') || trimmed.StartsWith('#'))
        {
            return IniLine.Comment(line);
        }

        // Section header
        if (trimmed.StartsWith('[') && trimmed.Contains(']'))
        {
            var endBracket = trimmed.IndexOf(']');
            var sectionName = trimmed[1..endBracket].Trim();
            return IniLine.Section(sectionName, line);
        }

        // Key-value pair
        var equalsIndex = trimmed.IndexOf('=');
        if (equalsIndex > 0)
        {
            var key = trimmed[..equalsIndex].Trim();
            var valueWithComment = trimmed[(equalsIndex + 1)..];

            // Check for inline comment (but not inside quoted values)
            string value;
            string? inlineComment = null;

            var commentIndex = FindInlineCommentIndex(valueWithComment);
            if (commentIndex >= 0)
            {
                value = valueWithComment[..commentIndex].Trim();
                inlineComment = valueWithComment[commentIndex..];
            }
            else
            {
                value = valueWithComment.Trim();
            }

            return IniLine.KeyValue(key, value, line, inlineComment);
        }

        // Unknown format, treat as comment/invalid line
        return IniLine.Comment(line);
    }

    private static int FindInlineCommentIndex(string value)
    {
        var inQuotes = false;
        char quoteChar = '\0';

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];

            if (!inQuotes && (c == '"' || c == '\''))
            {
                inQuotes = true;
                quoteChar = c;
            }
            else if (inQuotes && c == quoteChar)
            {
                inQuotes = false;
            }
            else if (!inQuotes && (c == ';' || c == '#'))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Gets all key-value pairs in a section.
    /// </summary>
    /// <param name="sectionName">The section name (case-insensitive).</param>
    /// <returns>A dictionary of key-value pairs.</returns>
    public IReadOnlyDictionary<string, string> GetSection(string sectionName)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var inSection = false;

        foreach (var line in _lines)
        {
            if (line.Type == IniLineType.Section)
            {
                inSection = string.Equals(line.SectionName, sectionName, StringComparison.OrdinalIgnoreCase);
            }
            else if (inSection && line.Type == IniLineType.KeyValue && line.Key is not null && line.Value is not null)
            {
                result[line.Key] = line.Value;
            }
        }

        return result;
    }

    /// <summary>
    /// Gets a value from a section.
    /// </summary>
    /// <param name="sectionName">The section name.</param>
    /// <param name="key">The key.</param>
    /// <returns>The value, or null if not found.</returns>
    public string? GetValue(string sectionName, string key)
    {
        var inSection = false;

        foreach (var line in _lines)
        {
            if (line.Type == IniLineType.Section)
            {
                inSection = string.Equals(line.SectionName, sectionName, StringComparison.OrdinalIgnoreCase);
            }
            else if (inSection && line.Type == IniLineType.KeyValue)
            {
                if (string.Equals(line.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return line.Value;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Sets a value in a section. Creates the section if it doesn't exist.
    /// </summary>
    /// <param name="sectionName">The section name.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void SetValue(string sectionName, string key, string value)
    {
        var sectionIndex = -1;
        var lastKeyIndex = -1;
        var keyIndex = -1;
        var inSection = false;

        for (var i = 0; i < _lines.Count; i++)
        {
            var line = _lines[i];

            if (line.Type == IniLineType.Section)
            {
                if (inSection)
                {
                    // We've left our target section without finding the key
                    break;
                }

                if (string.Equals(line.SectionName, sectionName, StringComparison.OrdinalIgnoreCase))
                {
                    sectionIndex = i;
                    inSection = true;
                }
            }
            else if (inSection)
            {
                if (line.Type == IniLineType.KeyValue)
                {
                    lastKeyIndex = i;

                    if (string.Equals(line.Key, key, StringComparison.OrdinalIgnoreCase))
                    {
                        keyIndex = i;
                        break;
                    }
                }
            }
        }

        if (keyIndex >= 0)
        {
            // Update existing key
            _lines[keyIndex] = _lines[keyIndex].WithValue(value);
        }
        else if (sectionIndex >= 0)
        {
            // Add new key to existing section
            var insertIndex = lastKeyIndex >= 0 ? lastKeyIndex + 1 : sectionIndex + 1;
            _lines.Insert(insertIndex, IniLine.KeyValue(key, value, $"{key}={value}"));
        }
        else
        {
            // Create new section
            if (_lines.Count > 0)
            {
                _lines.Add(IniLine.Empty());
            }
            _lines.Add(IniLine.Section(sectionName, $"[{sectionName}]"));
            _lines.Add(IniLine.KeyValue(key, value, $"{key}={value}"));
        }
    }

    /// <summary>
    /// Removes a key from a section.
    /// </summary>
    /// <param name="sectionName">The section name.</param>
    /// <param name="key">The key to remove.</param>
    /// <returns>True if the key was found and removed.</returns>
    public bool RemoveKey(string sectionName, string key)
    {
        var inSection = false;

        for (var i = 0; i < _lines.Count; i++)
        {
            var line = _lines[i];

            if (line.Type == IniLineType.Section)
            {
                inSection = string.Equals(line.SectionName, sectionName, StringComparison.OrdinalIgnoreCase);
            }
            else if (inSection && line.Type == IniLineType.KeyValue)
            {
                if (string.Equals(line.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    _lines.RemoveAt(i);
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Converts the document back to a string.
    /// </summary>
    /// <returns>The INI file content.</returns>
    public override string ToString()
    {
        var sb = new StringBuilder();

        for (var i = 0; i < _lines.Count; i++)
        {
            sb.Append(_lines[i].RawText);
            if (i < _lines.Count - 1)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Creates a deep copy of this document.
    /// </summary>
    /// <returns>A new document with the same content.</returns>
    public IniDocument Clone()
    {
        var doc = new IniDocument();
        doc._lines.AddRange(_lines);
        return doc;
    }
}
