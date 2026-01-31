namespace Wslr.Core.Parsing;

/// <summary>
/// Parses Linux /proc/stat output.
/// </summary>
public static class LinuxCpuStatParser
{
    /// <summary>
    /// Parses the output of 'cat /proc/stat' from a Linux system.
    /// </summary>
    /// <param name="statOutput">The raw output from /proc/stat.</param>
    /// <returns>A parsed <see cref="LinuxCpuStat"/> object, or null if parsing fails.</returns>
    public static LinuxCpuStat? Parse(string statOutput)
    {
        if (string.IsNullOrWhiteSpace(statOutput))
        {
            return null;
        }

        // Find the first line starting with "cpu " (aggregate CPU stats)
        var lines = statOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("cpu ", StringComparison.OrdinalIgnoreCase))
            {
                return ParseCpuLine(trimmed);
            }
        }

        return null;
    }

    private static LinuxCpuStat? ParseCpuLine(string line)
    {
        // Format: "cpu  10132153 290696 3084719 46828483 16683 0 25195 0 0 0"
        // Fields: cpu user nice system idle iowait irq softirq steal [guest] [guest_nice]
        // Note: guest and guest_nice are already included in user/nice, so we don't need them separately

        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Minimum required: cpu + user + nice + system + idle = 5 parts
        if (parts.Length < 5)
        {
            return null;
        }

        // Skip "cpu" prefix
        if (!long.TryParse(parts[1], out var user) ||
            !long.TryParse(parts[2], out var nice) ||
            !long.TryParse(parts[3], out var system) ||
            !long.TryParse(parts[4], out var idle))
        {
            return null;
        }

        // Optional fields (may not exist on older kernels)
        var ioWait = parts.Length > 5 && long.TryParse(parts[5], out var iw) ? iw : 0;
        var irq = parts.Length > 6 && long.TryParse(parts[6], out var ir) ? ir : 0;
        var softIrq = parts.Length > 7 && long.TryParse(parts[7], out var si) ? si : 0;
        var steal = parts.Length > 8 && long.TryParse(parts[8], out var st) ? st : 0;

        return new LinuxCpuStat
        {
            User = user,
            Nice = nice,
            System = system,
            Idle = idle,
            IoWait = ioWait,
            Irq = irq,
            SoftIrq = softIrq,
            Steal = steal
        };
    }
}
