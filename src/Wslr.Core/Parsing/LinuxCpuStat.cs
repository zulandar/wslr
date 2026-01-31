namespace Wslr.Core.Parsing;

/// <summary>
/// Represents CPU statistics parsed from Linux /proc/stat.
/// </summary>
public sealed record LinuxCpuStat
{
    /// <summary>
    /// Gets the time spent in user mode (in jiffies).
    /// </summary>
    public required long User { get; init; }

    /// <summary>
    /// Gets the time spent in user mode with low priority (nice) (in jiffies).
    /// </summary>
    public required long Nice { get; init; }

    /// <summary>
    /// Gets the time spent in system mode (in jiffies).
    /// </summary>
    public required long System { get; init; }

    /// <summary>
    /// Gets the time spent idle (in jiffies).
    /// </summary>
    public required long Idle { get; init; }

    /// <summary>
    /// Gets the time spent waiting for I/O to complete (in jiffies).
    /// </summary>
    public long IoWait { get; init; }

    /// <summary>
    /// Gets the time spent servicing hardware interrupts (in jiffies).
    /// </summary>
    public long Irq { get; init; }

    /// <summary>
    /// Gets the time spent servicing software interrupts (in jiffies).
    /// </summary>
    public long SoftIrq { get; init; }

    /// <summary>
    /// Gets the time stolen by other operating systems running in a virtualized environment (in jiffies).
    /// </summary>
    public long Steal { get; init; }

    /// <summary>
    /// Gets the total CPU time across all states (in jiffies).
    /// </summary>
    public long TotalTime => User + Nice + System + Idle + IoWait + Irq + SoftIrq + Steal;

    /// <summary>
    /// Gets the total idle time (Idle + IoWait) (in jiffies).
    /// </summary>
    public long IdleTime => Idle + IoWait;

    /// <summary>
    /// Gets the total active (non-idle) time (in jiffies).
    /// </summary>
    public long ActiveTime => TotalTime - IdleTime;
}
