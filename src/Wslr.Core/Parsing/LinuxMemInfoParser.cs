namespace Wslr.Core.Parsing;

/// <summary>
/// Parses Linux /proc/meminfo output.
/// </summary>
public static class LinuxMemInfoParser
{
    /// <summary>
    /// Parses the output of 'cat /proc/meminfo' from a Linux system.
    /// </summary>
    /// <param name="memInfoOutput">The raw output from /proc/meminfo.</param>
    /// <returns>A parsed <see cref="LinuxMemInfo"/> object, or null if parsing fails.</returns>
    public static LinuxMemInfo? Parse(string memInfoOutput)
    {
        if (string.IsNullOrWhiteSpace(memInfoOutput))
        {
            return null;
        }

        var values = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        var lines = memInfoOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parsed = ParseLine(line);
            if (parsed.HasValue)
            {
                values[parsed.Value.Key] = parsed.Value.Value;
            }
        }

        // Require at least MemTotal and MemFree
        if (!values.TryGetValue("MemTotal", out var memTotal) ||
            !values.TryGetValue("MemFree", out var memFree))
        {
            return null;
        }

        // MemAvailable may not exist on older kernels, fall back to calculation
        var memAvailable = values.GetValueOrDefault("MemAvailable",
            memFree + values.GetValueOrDefault("Buffers", 0) + values.GetValueOrDefault("Cached", 0));

        return new LinuxMemInfo
        {
            MemTotalKb = memTotal,
            MemFreeKb = memFree,
            MemAvailableKb = memAvailable,
            BuffersKb = values.GetValueOrDefault("Buffers", 0),
            CachedKb = values.GetValueOrDefault("Cached", 0),
            SwapTotalKb = values.GetValueOrDefault("SwapTotal", 0),
            SwapFreeKb = values.GetValueOrDefault("SwapFree", 0)
        };
    }

    private static (string Key, long Value)? ParseLine(string line)
    {
        // Format: "MemTotal:       16323740 kB"
        var colonIndex = line.IndexOf(':');
        if (colonIndex < 0)
        {
            return null;
        }

        var key = line[..colonIndex].Trim();
        var valuePart = line[(colonIndex + 1)..].Trim();

        // Remove "kB" suffix and parse the number
        var numberStr = valuePart
            .Replace("kB", "", StringComparison.OrdinalIgnoreCase)
            .Replace("KB", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        if (long.TryParse(numberStr, out var value))
        {
            return (key, value);
        }

        return null;
    }
}
