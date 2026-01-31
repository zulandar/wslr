namespace Wslr.Core.Parsing;

/// <summary>
/// Parses Linux df command output.
/// </summary>
public static class LinuxDiskUsageParser
{
    /// <summary>
    /// Parses the output of 'df -B1 /' from a Linux system.
    /// </summary>
    /// <param name="dfOutput">The raw output from df command.</param>
    /// <returns>A parsed <see cref="LinuxDiskUsage"/> object, or null if parsing fails.</returns>
    public static LinuxDiskUsage? Parse(string dfOutput)
    {
        if (string.IsNullOrWhiteSpace(dfOutput))
        {
            return null;
        }

        var lines = dfOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        // Need at least header + data line
        if (lines.Length < 2)
        {
            return null;
        }

        // Skip header line, parse first data line
        // Format: Filesystem     1B-blocks        Used   Available Use% Mounted on
        // Example: /dev/sdc       269490393088 8547123456 247181266944   4% /
        for (var i = 1; i < lines.Length; i++)
        {
            var result = ParseDataLine(lines[i]);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Parses the output of 'df -B1' (all filesystems) and returns the root filesystem usage.
    /// </summary>
    /// <param name="dfOutput">The raw output from df command.</param>
    /// <returns>A parsed <see cref="LinuxDiskUsage"/> for the root filesystem, or null if not found.</returns>
    public static LinuxDiskUsage? ParseRootFilesystem(string dfOutput)
    {
        if (string.IsNullOrWhiteSpace(dfOutput))
        {
            return null;
        }

        var lines = dfOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        // Find the line mounted on "/"
        foreach (var line in lines.Skip(1)) // Skip header
        {
            var result = ParseDataLine(line);
            if (result?.MountPoint == "/")
            {
                return result;
            }
        }

        return null;
    }

    private static LinuxDiskUsage? ParseDataLine(string line)
    {
        // Format: Filesystem     1B-blocks        Used   Available Use% Mounted on
        // Example: /dev/sdc       269490393088 8547123456 247181266944   4% /
        // Fields are whitespace-separated

        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Need at least: filesystem, total, used, available, use%, mountpoint
        if (parts.Length < 6)
        {
            return null;
        }

        var filesystem = parts[0];

        if (!long.TryParse(parts[1], out var totalBytes))
        {
            return null;
        }

        if (!long.TryParse(parts[2], out var usedBytes))
        {
            return null;
        }

        if (!long.TryParse(parts[3], out var availableBytes))
        {
            return null;
        }

        // parts[4] is Use% (e.g., "4%") - we calculate this ourselves
        // parts[5] is mount point
        var mountPoint = parts[5];

        return new LinuxDiskUsage
        {
            Filesystem = filesystem,
            TotalBytes = totalBytes,
            UsedBytes = usedBytes,
            AvailableBytes = availableBytes,
            MountPoint = mountPoint
        };
    }
}
