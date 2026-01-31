using Wslr.Core.Parsing;

namespace Wslr.UI.Services;

/// <summary>
/// Tracks CPU statistics per distribution and calculates CPU usage percentage from deltas.
/// </summary>
/// <remarks>
/// CPU percentage cannot be calculated from a single sample - it requires comparing
/// two consecutive samples to determine how much CPU time was used in the interval.
/// This class maintains the previous sample for each distribution to enable delta calculation.
/// </remarks>
public class DistributionCpuTracker
{
    private readonly object _lock = new();
    private readonly Dictionary<string, LinuxCpuStat> _previousStats = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Calculates CPU usage percentage for a distribution based on the delta from the previous sample.
    /// </summary>
    /// <param name="distributionName">The name of the distribution.</param>
    /// <param name="currentStat">The current CPU statistics.</param>
    /// <returns>
    /// The CPU usage percentage (0-100), or null if this is the first sample
    /// (no previous sample to compare against).
    /// </returns>
    public double? CalculateCpuPercent(string distributionName, LinuxCpuStat currentStat)
    {
        ArgumentNullException.ThrowIfNull(distributionName);
        ArgumentNullException.ThrowIfNull(currentStat);

        lock (_lock)
        {
            if (!_previousStats.TryGetValue(distributionName, out var previous))
            {
                // First sample - store it and return null
                _previousStats[distributionName] = currentStat;
                return null;
            }

            // Detect distribution restart: if current values are lower than previous,
            // the distribution was restarted and counters reset
            if (currentStat.TotalTime < previous.TotalTime)
            {
                // Reset state and treat this as first sample
                _previousStats[distributionName] = currentStat;
                return null;
            }

            var cpuPercent = CalculateDelta(previous, currentStat);
            _previousStats[distributionName] = currentStat;
            return cpuPercent;
        }
    }

    /// <summary>
    /// Clears the stored state for a specific distribution.
    /// </summary>
    /// <param name="distributionName">The name of the distribution to clear.</param>
    /// <remarks>
    /// Call this when a distribution is stopped to ensure fresh calculations
    /// when it starts again.
    /// </remarks>
    public void ClearDistribution(string distributionName)
    {
        ArgumentNullException.ThrowIfNull(distributionName);

        lock (_lock)
        {
            _previousStats.Remove(distributionName);
        }
    }

    /// <summary>
    /// Clears all stored distribution states.
    /// </summary>
    public void ClearAll()
    {
        lock (_lock)
        {
            _previousStats.Clear();
        }
    }

    /// <summary>
    /// Gets whether a previous sample exists for the specified distribution.
    /// </summary>
    /// <param name="distributionName">The name of the distribution.</param>
    /// <returns>True if a previous sample exists; otherwise, false.</returns>
    public bool HasPreviousSample(string distributionName)
    {
        ArgumentNullException.ThrowIfNull(distributionName);

        lock (_lock)
        {
            return _previousStats.ContainsKey(distributionName);
        }
    }

    /// <summary>
    /// Gets the number of distributions being tracked.
    /// </summary>
    public int TrackedDistributionCount
    {
        get
        {
            lock (_lock)
            {
                return _previousStats.Count;
            }
        }
    }

    private static double CalculateDelta(LinuxCpuStat previous, LinuxCpuStat current)
    {
        var totalDelta = current.TotalTime - previous.TotalTime;
        var idleDelta = current.IdleTime - previous.IdleTime;

        // Handle edge case where no time has passed
        if (totalDelta <= 0)
        {
            return 0;
        }

        var cpuPercent = 100.0 * (totalDelta - idleDelta) / totalDelta;

        // Clamp to valid range (could exceed 100% due to timing issues)
        return Math.Clamp(cpuPercent, 0, 100);
    }
}
