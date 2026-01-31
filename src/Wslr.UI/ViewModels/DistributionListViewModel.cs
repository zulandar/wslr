using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wslr.Core.Interfaces;
using Wslr.UI.Services;

namespace Wslr.UI.ViewModels;

/// <summary>
/// ViewModel for the distribution list view.
/// </summary>
public partial class DistributionListViewModel : ObservableObject, IDisposable
{
    private readonly IWslService _wslService;
    private readonly IDialogService _dialogService;
    private readonly IDistributionMonitorService _monitorService;
    private readonly IResourceMonitorService _resourceMonitorService;
    private readonly IDistributionResourceService _distributionResourceService;
    private readonly ISettingsService _settingsService;
    private readonly SynchronizationContext? _synchronizationContext;
    private readonly HashSet<string> _pinnedNames = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty]
    private ObservableCollection<DistributionItemViewModel> _distributions = [];

    [ObservableProperty]
    private ObservableCollection<DistributionItemViewModel> _pinnedDistributions = [];

    [ObservableProperty]
    private ObservableCollection<DistributionItemViewModel> _unpinnedDistributions = [];

    [ObservableProperty]
    private bool _hasPinnedDistributions;

    [ObservableProperty]
    private DistributionItemViewModel? _selectedDistribution;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isAutoRefreshEnabled;

    [ObservableProperty]
    private int _autoRefreshIntervalSeconds = 5;

    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    private bool _isGridView;

    [ObservableProperty]
    private bool _isListView;

    [ObservableProperty]
    private int _totalCpuUsage;

    [ObservableProperty]
    private double _totalMemoryUsage;

    [ObservableProperty]
    private double _totalDiskUsage;

    /// <summary>
    /// Gets the count of running distributions.
    /// </summary>
    public int RunningCount => Distributions.Count(d => d.IsRunning);

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributionListViewModel"/> class.
    /// </summary>
    /// <param name="wslService">The WSL service.</param>
    /// <param name="dialogService">The dialog service.</param>
    /// <param name="monitorService">The distribution monitor service.</param>
    /// <param name="resourceMonitorService">The resource monitor service.</param>
    /// <param name="distributionResourceService">The distribution resource service.</param>
    /// <param name="settingsService">The settings service.</param>
    public DistributionListViewModel(
        IWslService wslService,
        IDialogService dialogService,
        IDistributionMonitorService monitorService,
        IResourceMonitorService resourceMonitorService,
        IDistributionResourceService distributionResourceService,
        ISettingsService settingsService)
    {
        _wslService = wslService ?? throw new ArgumentNullException(nameof(wslService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _monitorService = monitorService ?? throw new ArgumentNullException(nameof(monitorService));
        _resourceMonitorService = resourceMonitorService ?? throw new ArgumentNullException(nameof(resourceMonitorService));
        _distributionResourceService = distributionResourceService ?? throw new ArgumentNullException(nameof(distributionResourceService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        // Capture the UI thread's synchronization context for thread marshaling
        _synchronizationContext = SynchronizationContext.Current;

        // Subscribe to monitor service events
        _monitorService.DistributionsRefreshed += OnDistributionsRefreshed;
        _monitorService.RefreshError += OnRefreshError;

        // Subscribe to resource monitor events
        _resourceMonitorService.ResourceUsageUpdated += OnResourceUsageUpdated;

        // Sync initial interval
        _monitorService.RefreshIntervalSeconds = _autoRefreshIntervalSeconds;
        _resourceMonitorService.RefreshIntervalSeconds = _autoRefreshIntervalSeconds;

        // Load saved view mode (default to List view)
        var savedViewMode = _settingsService.Get(SettingKeys.ViewMode, "List");
        _isGridView = savedViewMode == "Grid";
        _isListView = savedViewMode != "Grid";

        // Load pinned distributions
        var pinnedCsv = _settingsService.Get(SettingKeys.PinnedDistributions, string.Empty);
        if (!string.IsNullOrEmpty(pinnedCsv))
        {
            foreach (var name in pinnedCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                _pinnedNames.Add(name);
            }
        }
    }

    private void OnResourceUsageUpdated(object? sender, ResourceUsage usage)
    {
        // Marshal to UI thread if needed
        if (_synchronizationContext is not null)
        {
            _synchronizationContext.Post(_ =>
            {
                UpdateResourceUsageValues(usage);
            }, null);
        }
        else
        {
            UpdateResourceUsageValues(usage);
        }
    }

    private void UpdateResourceUsageValues(ResourceUsage usage)
    {
        TotalCpuUsage = (int)Math.Round(usage.CpuUsagePercent);
        TotalMemoryUsage = Math.Round(usage.MemoryUsageGb, 1);
        TotalDiskUsage = Math.Round(usage.TotalDiskUsageGb, 1);
    }

    partial void OnIsAutoRefreshEnabledChanged(bool value)
    {
        if (value)
        {
            _monitorService.StartMonitoring();
            _resourceMonitorService.StartMonitoring();
        }
        else
        {
            _monitorService.StopMonitoring();
            _resourceMonitorService.StopMonitoring();
        }
    }

    partial void OnAutoRefreshIntervalSecondsChanged(int value)
    {
        _monitorService.RefreshIntervalSeconds = value;
        _resourceMonitorService.RefreshIntervalSeconds = value;
    }

    partial void OnIsGridViewChanged(bool value)
    {
        if (value && IsListView)
        {
#pragma warning disable MVVMTK0034
            SetProperty(ref _isListView, false, nameof(IsListView));
#pragma warning restore MVVMTK0034
        }

        if (value)
        {
            _settingsService.Set(SettingKeys.ViewMode, "Grid");
            _settingsService.Save();
        }
    }

    partial void OnIsListViewChanged(bool value)
    {
        if (value && IsGridView)
        {
#pragma warning disable MVVMTK0034
            SetProperty(ref _isGridView, false, nameof(IsGridView));
#pragma warning restore MVVMTK0034
        }

        if (value)
        {
            _settingsService.Set(SettingKeys.ViewMode, "List");
            _settingsService.Save();
        }
    }

    private void OnDistributionsRefreshed(object? sender, EventArgs e)
    {
        // Marshal to UI thread if needed
        if (_synchronizationContext is not null)
        {
            _synchronizationContext.Post(_ =>
            {
                UpdateDistributionsFromMonitor();
                IsLoading = false;
            }, null);
        }
        else
        {
            UpdateDistributionsFromMonitor();
            IsLoading = false;
        }
    }

    private void OnRefreshError(object? sender, string errorMessage)
    {
        // Marshal to UI thread if needed
        if (_synchronizationContext is not null)
        {
            _synchronizationContext.Post(_ =>
            {
                ErrorMessage = $"Failed to load distributions: {errorMessage}";
                IsLoading = false;
            }, null);
        }
        else
        {
            ErrorMessage = $"Failed to load distributions: {errorMessage}";
            IsLoading = false;
        }
    }

    private void UpdateDistributionsFromMonitor()
    {
        var monitorDistributions = _monitorService.Distributions;
        var existingNames = Distributions.Select(d => d.Name).ToHashSet();
        var newNames = monitorDistributions.Select(d => d.Name).ToHashSet();

        // Remove distributions that no longer exist
        var toRemove = Distributions.Where(d => !newNames.Contains(d.Name)).ToList();
        foreach (var item in toRemove)
        {
            Distributions.Remove(item);
        }

        // Update or add distributions
        foreach (var distribution in monitorDistributions)
        {
            var isPinned = _pinnedNames.Contains(distribution.Name);
            var existing = Distributions.FirstOrDefault(d => d.Name == distribution.Name);
            if (existing is not null)
            {
                existing.UpdateFromModel(distribution);
                existing.IsPinned = isPinned;
            }
            else
            {
                Distributions.Add(DistributionItemViewModel.FromModel(distribution, isPinned));
            }
        }

        ErrorMessage = null;

        // Rebuild filtered collections for pinned/unpinned
        RebuildFilteredCollections();

        // Notify computed properties
        OnPropertyChanged(nameof(RunningCount));

        // Fetch memory for running distributions in background
        _ = FetchDistributionMemoryAsync();
    }

    private async Task FetchDistributionMemoryAsync()
    {
        var runningDistributions = Distributions.Where(d => d.IsRunning).ToList();
        if (runningDistributions.Count == 0)
        {
            return;
        }

        var names = runningDistributions.Select(d => d.Name).ToList();

        try
        {
            var memoryUsages = await _distributionResourceService.GetMemoryUsageAsync(names);

            // Update on UI thread
            if (_synchronizationContext is not null)
            {
                _synchronizationContext.Post(_ => ApplyMemoryUsages(memoryUsages), null);
            }
            else
            {
                ApplyMemoryUsages(memoryUsages);
            }
        }
        catch
        {
            // Ignore errors fetching memory - it's not critical
        }
    }

    private void ApplyMemoryUsages(IReadOnlyDictionary<string, double?> memoryUsages)
    {
        foreach (var (name, memory) in memoryUsages)
        {
            var distribution = Distributions.FirstOrDefault(d =>
                d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (distribution is not null)
            {
                distribution.MemoryUsageGb = memory.HasValue ? Math.Round(memory.Value, 2) : null;
            }
        }
    }

    /// <summary>
    /// Loads the distribution list.
    /// </summary>
    [RelayCommand]
    private async Task LoadDistributionsAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoading)
        {
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            // Refresh both distributions and resource usage
            await Task.WhenAll(
                _monitorService.RefreshAsync(cancellationToken),
                _resourceMonitorService.RefreshAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
            IsLoading = false;
        }
    }

    /// <summary>
    /// Starts the selected distribution.
    /// </summary>
    [RelayCommand]
    private async Task StartDistributionAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedDistribution is null)
        {
            return;
        }

        try
        {
            await _wslService.StartDistributionAsync(SelectedDistribution.Name, cancellationToken);
            await _monitorService.RefreshAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to start distribution: {ex.Message}";
        }
    }

    /// <summary>
    /// Stops the selected distribution.
    /// </summary>
    [RelayCommand]
    private async Task StopDistributionAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedDistribution is null)
        {
            return;
        }

        try
        {
            await _wslService.TerminateDistributionAsync(SelectedDistribution.Name, cancellationToken);
            await _monitorService.RefreshAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to stop distribution: {ex.Message}";
        }
    }

    /// <summary>
    /// Deletes the selected distribution.
    /// </summary>
    [RelayCommand]
    private async Task DeleteDistributionAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedDistribution is null)
        {
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Distribution",
            $"Are you sure you want to delete '{SelectedDistribution.Name}'? This action cannot be undone.");

        if (!confirmed)
        {
            return;
        }

        try
        {
            await _wslService.UnregisterDistributionAsync(SelectedDistribution.Name, cancellationToken);
            await _monitorService.RefreshAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete distribution: {ex.Message}";
        }
    }

    /// <summary>
    /// Shuts down all WSL distributions.
    /// </summary>
    [RelayCommand]
    private async Task ShutdownAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _wslService.ShutdownAsync(cancellationToken);
            await _monitorService.RefreshAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to shutdown WSL: {ex.Message}";
        }
    }

    /// <summary>
    /// Toggles the pinned state for a distribution.
    /// </summary>
    /// <param name="distribution">The distribution to toggle.</param>
    [RelayCommand]
    private void TogglePin(DistributionItemViewModel? distribution)
    {
        if (distribution is null)
        {
            return;
        }

        if (_pinnedNames.Contains(distribution.Name))
        {
            _pinnedNames.Remove(distribution.Name);
            distribution.IsPinned = false;
        }
        else
        {
            _pinnedNames.Add(distribution.Name);
            distribution.IsPinned = true;
        }

        // Save to settings
        var pinnedCsv = string.Join(",", _pinnedNames);
        _settingsService.Set(SettingKeys.PinnedDistributions, pinnedCsv);
        _settingsService.Save();

        // Rebuild filtered collections
        RebuildFilteredCollections();
    }

    private void RebuildFilteredCollections()
    {
        PinnedDistributions.Clear();
        UnpinnedDistributions.Clear();

        foreach (var dist in Distributions)
        {
            if (dist.IsPinned)
            {
                PinnedDistributions.Add(dist);
            }
            else
            {
                UnpinnedDistributions.Add(dist);
            }
        }

        HasPinnedDistributions = PinnedDistributions.Count > 0;
    }

    /// <summary>
    /// Sets the selected distribution as the default.
    /// </summary>
    [RelayCommand]
    private async Task SetDefaultAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedDistribution is null || SelectedDistribution.IsDefault)
        {
            return;
        }

        try
        {
            await _wslService.SetDefaultDistributionAsync(SelectedDistribution.Name, cancellationToken);
            await _monitorService.RefreshAsync(cancellationToken);
            await _dialogService.ShowInfoAsync("Default Set", $"{SelectedDistribution.Name} is now the default distribution.");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to set default: {ex.Message}";
        }
    }

    /// <summary>
    /// Exports the selected distribution to a tar file.
    /// </summary>
    [RelayCommand]
    private async Task ExportDistributionAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedDistribution is null)
        {
            return;
        }

        var defaultFileName = $"{SelectedDistribution.Name}-{DateTime.Now:yyyy-MM-dd}.tar";
        var filePath = await _dialogService.ShowSaveFileDialogAsync(
            "Export Distribution",
            defaultFileName,
            "Tar files|*.tar|All files|*.*");

        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            await _wslService.ExportDistributionAsync(SelectedDistribution.Name, filePath, null, cancellationToken);
            await _dialogService.ShowInfoAsync("Export Complete", $"Distribution exported to:\n{filePath}");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to export distribution: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Imports a distribution from a tar file.
    /// </summary>
    [RelayCommand]
    private async Task ImportDistributionAsync(CancellationToken cancellationToken = default)
    {
        // Select tar file
        var tarPath = await _dialogService.ShowOpenFileDialogAsync(
            "Select Distribution Archive",
            "Tar files|*.tar|All files|*.*");

        if (string.IsNullOrEmpty(tarPath))
        {
            return;
        }

        // Get distribution name from user
        var fileName = System.IO.Path.GetFileNameWithoutExtension(tarPath);
        // Remove date suffix if present (e.g., "Ubuntu-2026-01-31" -> "Ubuntu")
        var suggestedName = System.Text.RegularExpressions.Regex.Replace(fileName, @"-\d{4}-\d{2}-\d{2}$", "");

        // For now, use a simple approach - in future could have a full dialog
        // Check if name already exists
        var existingNames = Distributions.Select(d => d.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var baseName = suggestedName;
        var counter = 1;
        while (existingNames.Contains(suggestedName))
        {
            suggestedName = $"{baseName}-{counter++}";
        }

        // Select install location
        var defaultLocation = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "wsl",
            suggestedName);

        var installLocation = await _dialogService.ShowFolderBrowserDialogAsync("Select Install Location");

        if (string.IsNullOrEmpty(installLocation))
        {
            // User cancelled, use default
            installLocation = defaultLocation;
        }

        // Confirm import
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Import Distribution",
            $"Import distribution with the following settings?\n\n" +
            $"Name: {suggestedName}\n" +
            $"Source: {tarPath}\n" +
            $"Location: {installLocation}\n" +
            $"WSL Version: 2");

        if (!confirmed)
        {
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            // Ensure install directory exists
            System.IO.Directory.CreateDirectory(installLocation);

            await _wslService.ImportDistributionAsync(suggestedName, installLocation, tarPath, 2, null, cancellationToken);
            await _monitorService.RefreshAsync(cancellationToken);
            await _dialogService.ShowInfoAsync("Import Complete", $"Distribution '{suggestedName}' imported successfully.");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to import distribution: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Cleans up resources.
    /// </summary>
    public void Dispose()
    {
        _monitorService.DistributionsRefreshed -= OnDistributionsRefreshed;
        _monitorService.RefreshError -= OnRefreshError;
        _resourceMonitorService.ResourceUsageUpdated -= OnResourceUsageUpdated;
    }
}
