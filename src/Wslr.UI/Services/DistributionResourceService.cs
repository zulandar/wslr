using Wslr.Core.Interfaces;
using Wslr.Core.Parsing;

namespace Wslr.UI.Services;

/// <summary>
/// Implementation of <see cref="IDistributionResourceService"/> that fetches resource
/// usage by executing commands inside WSL distributions.
/// </summary>
public class DistributionResourceService : IDistributionResourceService
{
    private readonly IWslService _wslService;
    private readonly DistributionCpuTracker _cpuTracker = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributionResourceService"/> class.
    /// </summary>
    /// <param name="wslService">The WSL service.</param>
    public DistributionResourceService(IWslService wslService)
    {
        _wslService = wslService ?? throw new ArgumentNullException(nameof(wslService));
    }

    /// <inheritdoc />
    public async Task<double?> GetMemoryUsageAsync(string distributionName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distributionName);

        try
        {
            var result = await _wslService.ExecuteCommandAsync(
                distributionName,
                "cat /proc/meminfo",
                cancellationToken);

            if (!result.IsSuccess)
            {
                return null;
            }

            var memInfo = LinuxMemInfoParser.Parse(result.StandardOutput);
            return memInfo?.UsedMemoryGb;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Distribution may not be running or command failed
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, double?>> GetMemoryUsageAsync(
        IEnumerable<string> distributionNames,
        CancellationToken cancellationToken = default)
    {
        var names = distributionNames.ToList();
        var results = new Dictionary<string, double?>(StringComparer.OrdinalIgnoreCase);

        // Fetch memory for all distributions in parallel
        var tasks = names.Select(async name =>
        {
            var memory = await GetMemoryUsageAsync(name, cancellationToken);
            return (Name: name, Memory: memory);
        });

        var completedTasks = await Task.WhenAll(tasks);

        foreach (var (name, memory) in completedTasks)
        {
            results[name] = memory;
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<double?> GetCpuUsageAsync(string distributionName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distributionName);

        try
        {
            var result = await _wslService.ExecuteCommandAsync(
                distributionName,
                "cat /proc/stat",
                cancellationToken);

            if (!result.IsSuccess)
            {
                return null;
            }

            var cpuStat = LinuxCpuStatParser.Parse(result.StandardOutput);
            if (cpuStat is null)
            {
                return null;
            }

            return _cpuTracker.CalculateCpuPercent(distributionName, cpuStat);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Distribution may not be running or command failed
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, double?>> GetCpuUsageAsync(
        IEnumerable<string> distributionNames,
        CancellationToken cancellationToken = default)
    {
        var names = distributionNames.ToList();
        var results = new Dictionary<string, double?>(StringComparer.OrdinalIgnoreCase);

        // Fetch CPU for all distributions in parallel
        var tasks = names.Select(async name =>
        {
            var cpu = await GetCpuUsageAsync(name, cancellationToken);
            return (Name: name, Cpu: cpu);
        });

        var completedTasks = await Task.WhenAll(tasks);

        foreach (var (name, cpu) in completedTasks)
        {
            results[name] = cpu;
        }

        return results;
    }

    /// <inheritdoc />
    public void ClearCpuState(string distributionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distributionName);
        _cpuTracker.ClearDistribution(distributionName);
    }

    /// <inheritdoc />
    public void ClearAllCpuState()
    {
        _cpuTracker.ClearAll();
    }
}
