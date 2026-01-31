namespace Wslr.Core.Models;

/// <summary>
/// Represents the [wsl2] section settings in .wslconfig.
/// </summary>
public sealed record Wsl2Settings
{
    /// <summary>
    /// Gets the kernel path for a custom Linux kernel.
    /// </summary>
    public string? Kernel { get; init; }

    /// <summary>
    /// Gets the amount of memory to assign to the WSL 2 VM.
    /// </summary>
    public string? Memory { get; init; }

    /// <summary>
    /// Gets the number of logical processors to assign to the WSL 2 VM.
    /// </summary>
    public int? Processors { get; init; }

    /// <summary>
    /// Gets a value indicating whether localhost forwarding is enabled.
    /// </summary>
    public bool? LocalhostForwarding { get; init; }

    /// <summary>
    /// Gets the path to the kernel command line.
    /// </summary>
    public string? KernelCommandLine { get; init; }

    /// <summary>
    /// Gets a value indicating whether safe mode is enabled.
    /// </summary>
    public bool? SafeMode { get; init; }

    /// <summary>
    /// Gets the swap space size.
    /// </summary>
    public string? Swap { get; init; }

    /// <summary>
    /// Gets the swap file path.
    /// </summary>
    public string? SwapFile { get; init; }

    /// <summary>
    /// Gets a value indicating whether page reporting is enabled.
    /// </summary>
    public bool? PageReporting { get; init; }

    /// <summary>
    /// Gets a value indicating whether GUI applications are enabled.
    /// </summary>
    public bool? GuiApplications { get; init; }

    /// <summary>
    /// Gets a value indicating whether debug console is enabled.
    /// </summary>
    public bool? DebugConsole { get; init; }

    /// <summary>
    /// Gets a value indicating whether nested virtualization is enabled.
    /// </summary>
    public bool? NestedVirtualization { get; init; }

    /// <summary>
    /// Gets the VM idle timeout in milliseconds.
    /// </summary>
    public int? VmIdleTimeout { get; init; }

    /// <summary>
    /// Gets a value indicating whether DNS tunneling is enabled.
    /// </summary>
    public bool? DnsTunneling { get; init; }

    /// <summary>
    /// Gets a value indicating whether firewall is enabled.
    /// </summary>
    public bool? Firewall { get; init; }

    /// <summary>
    /// Gets the network mode (e.g., "nat", "mirrored").
    /// </summary>
    public string? NetworkingMode { get; init; }

    /// <summary>
    /// Gets additional settings not explicitly defined.
    /// </summary>
    public IReadOnlyDictionary<string, string> AdditionalSettings { get; init; } =
        new Dictionary<string, string>();
}
