using System.Text.RegularExpressions;
using Wslr.Core.Models;

namespace Wslr.Infrastructure.Parsing;

/// <summary>
/// Parses output from WSL commands.
/// </summary>
public static partial class WslOutputParser
{
    /// <summary>
    /// Parses the output of 'wsl --list --verbose' command.
    /// </summary>
    /// <param name="output">The raw output from the command.</param>
    /// <returns>A list of parsed WSL distributions.</returns>
    public static IReadOnlyList<WslDistribution> ParseListVerbose(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return [];
        }

        var distributions = new List<WslDistribution>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Skip header line(s) - look for lines that start with spaces or asterisk
        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Skip header lines (typically contain "NAME" and "STATE")
            if (string.IsNullOrWhiteSpace(trimmed) ||
                trimmed.Contains("NAME", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("STATE", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var distribution = ParseDistributionLine(line);
            if (distribution is not null)
            {
                distributions.Add(distribution);
            }
        }

        return distributions;
    }

    /// <summary>
    /// Parses the output of 'wsl --list --online' command.
    /// </summary>
    /// <param name="output">The raw output from the command.</param>
    /// <returns>A list of available online distributions.</returns>
    public static IReadOnlyList<OnlineDistribution> ParseListOnline(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return [];
        }

        var distributions = new List<OnlineDistribution>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var foundHeader = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Look for the header line that contains "NAME" and skip everything before it
            if (!foundHeader)
            {
                if (trimmed.Contains("NAME", StringComparison.OrdinalIgnoreCase) &&
                    trimmed.Contains("FRIENDLY", StringComparison.OrdinalIgnoreCase))
                {
                    foundHeader = true;
                }
                continue;
            }

            // Skip empty lines and separator lines
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.All(c => c == '-'))
            {
                continue;
            }

            var distribution = ParseOnlineDistributionLine(trimmed);
            if (distribution is not null)
            {
                distributions.Add(distribution);
            }
        }

        return distributions;
    }

    private static WslDistribution? ParseDistributionLine(string line)
    {
        // Format: "* Ubuntu            Running  2" or "  Debian            Stopped  1"
        // The asterisk indicates the default distribution

        var isDefault = line.TrimStart().StartsWith('*');
        var cleanLine = line.Replace("*", " ").Trim();

        // Split on whitespace, but we need to handle multiple spaces
        var parts = WhitespaceRegex().Split(cleanLine)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        if (parts.Length < 3)
        {
            return null;
        }

        var name = parts[0];
        var stateStr = parts[1];
        var versionStr = parts[2];

        if (!int.TryParse(versionStr, out var version))
        {
            version = 2; // Default to WSL 2
        }

        var state = ParseState(stateStr);

        return new WslDistribution
        {
            Name = name,
            State = state,
            Version = version,
            IsDefault = isDefault
        };
    }

    private static OnlineDistribution? ParseOnlineDistributionLine(string line)
    {
        // Format: "Ubuntu-24.04    Ubuntu 24.04 LTS"
        var parts = WhitespaceRegex().Split(line)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        if (parts.Length < 2)
        {
            return null;
        }

        var name = parts[0];
        var friendlyName = string.Join(" ", parts.Skip(1));

        return new OnlineDistribution
        {
            Name = name,
            FriendlyName = friendlyName
        };
    }

    private static DistributionState ParseState(string state)
    {
        return state.ToUpperInvariant() switch
        {
            "RUNNING" => DistributionState.Running,
            "STOPPED" => DistributionState.Stopped,
            "INSTALLING" => DistributionState.Installing,
            _ => DistributionState.Unknown
        };
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
