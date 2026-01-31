using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace Wslr.UI.Services;

/// <summary>
/// Implementation of <see cref="IResourceMonitorService"/> that monitors WSL2 resource usage.
/// </summary>
/// <remarks>
/// CPU and Memory are monitored via the 'vmmem' process which represents the WSL2 VM.
/// Disk usage is calculated from VHDX file sizes, located via the Windows Registry.
/// </remarks>
public class ResourceMonitorService : IResourceMonitorService
{
    private const string LxssRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Lxss";
    private const string VmmemProcessName = "vmmem";
    private const double BytesToGb = 1024.0 * 1024.0 * 1024.0;

    private readonly object _lock = new();
    private readonly Dictionary<string, double> _diskUsageCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly PerformanceCounter? _cpuCounter;

    private Timer? _timer;
    private ResourceUsage _currentUsage = ResourceUsage.Empty;
    private int _refreshIntervalSeconds = 5;
    private bool _isMonitoring;
    private bool _disposed;
    private DateTime _lastDiskRefresh = DateTime.MinValue;
    private TimeSpan _diskCacheDuration = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceMonitorService"/> class.
    /// </summary>
    public ResourceMonitorService()
    {
        try
        {
            // Create a performance counter for CPU usage of the vmmem process
            // This may fail if performance counters are not available
            _cpuCounter = new PerformanceCounter("Process", "% Processor Time", VmmemProcessName, true);
            // First call always returns 0, so initialize it
            _ = _cpuCounter.NextValue();
        }
        catch
        {
            // Performance counters not available, will fall back to alternative method
            _cpuCounter = null;
        }
    }

    /// <inheritdoc />
    public ResourceUsage CurrentUsage
    {
        get
        {
            lock (_lock)
            {
                return _currentUsage;
            }
        }
    }

    /// <inheritdoc />
    public bool IsMonitoring
    {
        get
        {
            lock (_lock)
            {
                return _isMonitoring;
            }
        }
    }

    /// <inheritdoc />
    public int RefreshIntervalSeconds
    {
        get => _refreshIntervalSeconds;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Refresh interval must be positive.");
            }

            _refreshIntervalSeconds = value;

            // Update timer if monitoring
            if (_isMonitoring)
            {
                _timer?.Change(TimeSpan.Zero, TimeSpan.FromSeconds(value));
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<ResourceUsage>? ResourceUsageUpdated;

    /// <inheritdoc />
    public event EventHandler<string>? MonitoringError;

    /// <inheritdoc />
    public void StartMonitoring()
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            if (_isMonitoring)
            {
                return;
            }

            _isMonitoring = true;
            _timer = new Timer(
                async _ => await RefreshInternalAsync(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(_refreshIntervalSeconds));
        }
    }

    /// <inheritdoc />
    public void StopMonitoring()
    {
        lock (_lock)
        {
            if (!_isMonitoring)
            {
                return;
            }

            _isMonitoring = false;
            _timer?.Dispose();
            _timer = null;
        }
    }

    /// <inheritdoc />
    public async Task<ResourceUsage> RefreshAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return await RefreshInternalAsync(cancellationToken);
    }

    /// <inheritdoc />
    public double? GetDistributionDiskUsage(string distributionName)
    {
        lock (_lock)
        {
            return _diskUsageCache.TryGetValue(distributionName, out var usage) ? usage : null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopMonitoring();
        _cpuCounter?.Dispose();

        lock (_lock)
        {
            _diskUsageCache.Clear();
            _currentUsage = ResourceUsage.Empty;
        }

        GC.SuppressFinalize(this);
    }

    private async Task<ResourceUsage> RefreshInternalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var (cpuUsage, memoryUsage, isRunning) = await Task.Run(() => GetVmmemMetrics(), cancellationToken);
            var diskUsage = await Task.Run(() => GetDiskUsage(cancellationToken), cancellationToken);

            var usage = new ResourceUsage
            {
                CpuUsagePercent = cpuUsage,
                MemoryUsageGb = memoryUsage,
                TotalDiskUsageGb = diskUsage.Values.Sum(),
                DiskUsageByDistribution = diskUsage,
                IsWslRunning = isRunning,
                Timestamp = DateTime.UtcNow
            };

            lock (_lock)
            {
                _currentUsage = usage;
            }

            ResourceUsageUpdated?.Invoke(this, usage);
            return usage;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            MonitoringError?.Invoke(this, ex.Message);
            return _currentUsage;
        }
    }

    private (double cpu, double memory, bool isRunning) GetVmmemMetrics()
    {
        try
        {
            var processes = Process.GetProcessesByName(VmmemProcessName);

            if (processes.Length == 0)
            {
                return (0, 0, false);
            }

            double totalMemoryBytes = 0;
            foreach (var process in processes)
            {
                try
                {
                    totalMemoryBytes += process.WorkingSet64;
                }
                finally
                {
                    process.Dispose();
                }
            }

            // Get CPU usage from performance counter if available
            double cpuUsage = 0;
            if (_cpuCounter is not null)
            {
                try
                {
                    // Normalize by processor count to get 0-100%
                    var rawValue = _cpuCounter.NextValue();
                    cpuUsage = rawValue / Environment.ProcessorCount;
                }
                catch
                {
                    // Counter may become invalid if process restarts
                }
            }

            return (Math.Round(cpuUsage, 1), totalMemoryBytes / BytesToGb, true);
        }
        catch
        {
            return (0, 0, false);
        }
    }

    private Dictionary<string, double> GetDiskUsage(CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        // Use cache if recent
        var now = DateTime.UtcNow;
        if (now - _lastDiskRefresh < _diskCacheDuration)
        {
            lock (_lock)
            {
                return new Dictionary<string, double>(_diskUsageCache, StringComparer.OrdinalIgnoreCase);
            }
        }

        try
        {
            using var lxssKey = Registry.CurrentUser.OpenSubKey(LxssRegistryPath, false);
            if (lxssKey is null)
            {
                return result;
            }

            foreach (var subKeyName in lxssKey.GetSubKeyNames())
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var distroKey = lxssKey.OpenSubKey(subKeyName, false);
                if (distroKey is null)
                {
                    continue;
                }

                var distributionName = distroKey.GetValue("DistributionName") as string;
                var basePath = distroKey.GetValue("BasePath") as string;

                if (string.IsNullOrEmpty(distributionName) || string.IsNullOrEmpty(basePath))
                {
                    continue;
                }

                // The VHDX file is typically at BasePath\ext4.vhdx
                var vhdxPath = Path.Combine(basePath, "ext4.vhdx");

                // Handle UNC-style paths that WSL uses (\\?\...)
                if (vhdxPath.StartsWith(@"\\?\", StringComparison.Ordinal))
                {
                    vhdxPath = vhdxPath[4..];
                }

                if (File.Exists(vhdxPath))
                {
                    try
                    {
                        var fileInfo = new FileInfo(vhdxPath);
                        result[distributionName] = fileInfo.Length / BytesToGb;
                    }
                    catch
                    {
                        // File may be locked or inaccessible
                    }
                }
            }

            // Update cache
            lock (_lock)
            {
                _diskUsageCache.Clear();
                foreach (var kvp in result)
                {
                    _diskUsageCache[kvp.Key] = kvp.Value;
                }
                _lastDiskRefresh = now;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Registry access failed, return empty
        }

        return result;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ResourceMonitorService));
        }
    }
}
