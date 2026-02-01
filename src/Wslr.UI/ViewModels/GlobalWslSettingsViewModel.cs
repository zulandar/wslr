using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wslr.Core.Interfaces;
using Wslr.Core.Models;
using Wslr.UI.Services;

namespace Wslr.UI.ViewModels;

/// <summary>
/// ViewModel for editing global WSL settings (.wslconfig).
/// </summary>
public partial class GlobalWslSettingsViewModel : ObservableObject
{
    private readonly IWslConfigService _configService;
    private readonly IDialogService _dialogService;
    private WslConfig _originalConfig = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _successMessage;

    [ObservableProperty]
    private bool _configExists;

    // WSL2 Settings
    [ObservableProperty]
    private string? _memory;

    [ObservableProperty]
    private int? _processors;

    [ObservableProperty]
    private string? _swap;

    [ObservableProperty]
    private string? _swapFile;

    [ObservableProperty]
    private bool _localhostForwarding = true;

    [ObservableProperty]
    private bool _guiApplications = true;

    [ObservableProperty]
    private bool _debugConsole;

    [ObservableProperty]
    private bool _nestedVirtualization = true;

    [ObservableProperty]
    private int? _vmIdleTimeout;

    [ObservableProperty]
    private string? _kernelPath;

    [ObservableProperty]
    private string? _kernelCommandLine;

    [ObservableProperty]
    private bool _pageReporting = true;

    [ObservableProperty]
    private bool _dnsTunneling = true;

    [ObservableProperty]
    private bool _firewall = true;

    [ObservableProperty]
    private string _networkingMode = "nat";

    // Experimental Settings
    [ObservableProperty]
    private string _autoMemoryReclaim = "disabled";

    [ObservableProperty]
    private bool _sparseVhd;

    [ObservableProperty]
    private bool _useWindowsDnsCache = true;

    [ObservableProperty]
    private bool _bestEffortDnsParsing;

    // Computed properties
    public int MaxProcessors => Environment.ProcessorCount;

    public string ConfigPath => _configService.ConfigPath;

    public IReadOnlyList<string> NetworkingModes { get; } = ["nat", "mirrored"];

    public IReadOnlyList<string> AutoMemoryReclaimOptions { get; } = ["disabled", "gradual", "dropcache"];

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalWslSettingsViewModel"/> class.
    /// </summary>
    public GlobalWslSettingsViewModel(IWslConfigService configService, IDialogService dialogService)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    }

    /// <summary>
    /// Loads the current configuration.
    /// </summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            ConfigExists = _configService.ConfigExists;
            _originalConfig = await _configService.ReadConfigAsync();
            ApplyConfigToViewModel(_originalConfig);
            IsDirty = false;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load configuration: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Saves the current configuration.
    /// </summary>
    [RelayCommand]
    public async Task SaveAsync()
    {
        if (!IsDirty)
        {
            return;
        }

        IsSaving = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var config = BuildConfigFromViewModel();

            // Validate before saving
            var validation = _configService.Validate(config);
            if (!validation.IsValid)
            {
                ErrorMessage = string.Join("\n", validation.Errors.Select(e => e.Message));
                return;
            }

            // Create backup before saving
            await _configService.CreateBackupAsync();

            // Save the configuration
            await _configService.WriteConfigAsync(config);

            _originalConfig = config;
            IsDirty = false;
            ConfigExists = true;
            SuccessMessage = "Settings saved. Restart WSL for changes to take effect.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save configuration: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// Reverts changes to the last saved state.
    /// </summary>
    [RelayCommand]
    public void Cancel()
    {
        ApplyConfigToViewModel(_originalConfig);
        IsDirty = false;
        ErrorMessage = null;
        SuccessMessage = null;
    }

    /// <summary>
    /// Resets all settings to defaults.
    /// </summary>
    [RelayCommand]
    public async Task ResetToDefaultsAsync()
    {
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Reset to Defaults",
            "Are you sure you want to reset all settings to their default values?");

        if (!confirmed)
        {
            return;
        }

        ApplyConfigToViewModel(new WslConfig());
        IsDirty = true;
    }

    private void ApplyConfigToViewModel(WslConfig config)
    {
        // WSL2 settings
        Memory = config.Wsl2.Memory;
        Processors = config.Wsl2.Processors;
        Swap = config.Wsl2.Swap;
        SwapFile = config.Wsl2.SwapFile;
        LocalhostForwarding = config.Wsl2.LocalhostForwarding ?? true;
        GuiApplications = config.Wsl2.GuiApplications ?? true;
        DebugConsole = config.Wsl2.DebugConsole ?? false;
        NestedVirtualization = config.Wsl2.NestedVirtualization ?? true;
        VmIdleTimeout = config.Wsl2.VmIdleTimeout;
        KernelPath = config.Wsl2.Kernel;
        KernelCommandLine = config.Wsl2.KernelCommandLine;
        PageReporting = config.Wsl2.PageReporting ?? true;
        DnsTunneling = config.Wsl2.DnsTunneling ?? true;
        Firewall = config.Wsl2.Firewall ?? true;
        NetworkingMode = config.Wsl2.NetworkingMode ?? "nat";

        // Experimental settings
        AutoMemoryReclaim = config.Experimental.AutoMemoryReclaim ?? "disabled";
        SparseVhd = config.Experimental.SparseVhd ?? false;
        UseWindowsDnsCache = config.Experimental.UseWindowsDnsCache ?? true;
        BestEffortDnsParsing = config.Experimental.BestEffortDnsParsing ?? false;
    }

    private WslConfig BuildConfigFromViewModel()
    {
        return new WslConfig
        {
            Wsl2 = new Wsl2Settings
            {
                Memory = string.IsNullOrWhiteSpace(Memory) ? null : Memory,
                Processors = Processors,
                Swap = string.IsNullOrWhiteSpace(Swap) ? null : Swap,
                SwapFile = string.IsNullOrWhiteSpace(SwapFile) ? null : SwapFile,
                LocalhostForwarding = LocalhostForwarding,
                GuiApplications = GuiApplications,
                DebugConsole = DebugConsole,
                NestedVirtualization = NestedVirtualization,
                VmIdleTimeout = VmIdleTimeout,
                Kernel = string.IsNullOrWhiteSpace(KernelPath) ? null : KernelPath,
                KernelCommandLine = string.IsNullOrWhiteSpace(KernelCommandLine) ? null : KernelCommandLine,
                PageReporting = PageReporting,
                DnsTunneling = DnsTunneling,
                Firewall = Firewall,
                NetworkingMode = NetworkingMode
            },
            Experimental = new ExperimentalSettings
            {
                AutoMemoryReclaim = AutoMemoryReclaim == "disabled" ? null : AutoMemoryReclaim,
                SparseVhd = SparseVhd ? true : null,
                UseWindowsDnsCache = UseWindowsDnsCache,
                BestEffortDnsParsing = BestEffortDnsParsing ? true : null
            }
        };
    }

    // Property change handlers to track dirty state
    partial void OnMemoryChanged(string? value) => MarkDirty();
    partial void OnProcessorsChanged(int? value) => MarkDirty();
    partial void OnSwapChanged(string? value) => MarkDirty();
    partial void OnSwapFileChanged(string? value) => MarkDirty();
    partial void OnLocalhostForwardingChanged(bool value) => MarkDirty();
    partial void OnGuiApplicationsChanged(bool value) => MarkDirty();
    partial void OnDebugConsoleChanged(bool value) => MarkDirty();
    partial void OnNestedVirtualizationChanged(bool value) => MarkDirty();
    partial void OnVmIdleTimeoutChanged(int? value) => MarkDirty();
    partial void OnKernelPathChanged(string? value) => MarkDirty();
    partial void OnKernelCommandLineChanged(string? value) => MarkDirty();
    partial void OnPageReportingChanged(bool value) => MarkDirty();
    partial void OnDnsTunnelingChanged(bool value) => MarkDirty();
    partial void OnFirewallChanged(bool value) => MarkDirty();
    partial void OnNetworkingModeChanged(string value) => MarkDirty();
    partial void OnAutoMemoryReclaimChanged(string value) => MarkDirty();
    partial void OnSparseVhdChanged(bool value) => MarkDirty();
    partial void OnUseWindowsDnsCacheChanged(bool value) => MarkDirty();
    partial void OnBestEffortDnsParsingChanged(bool value) => MarkDirty();

    private void MarkDirty()
    {
        if (!IsLoading)
        {
            IsDirty = true;
            SuccessMessage = null;
        }
    }
}
