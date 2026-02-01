using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wslr.Core.Interfaces;
using Wslr.Core.Models;
using Wslr.UI.Services;

namespace Wslr.UI.ViewModels;

/// <summary>
/// ViewModel for editing per-distribution WSL settings (wsl.conf).
/// </summary>
public partial class DistroSettingsViewModel : ObservableObject
{
    private readonly IWslDistroConfigService _configService;
    private readonly IWslService _wslService;
    private readonly IDialogService _dialogService;
    private WslDistroConfig _originalConfig = new();

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
    private ObservableCollection<string> _distributions = [];

    [ObservableProperty]
    private string? _selectedDistribution;

    [ObservableProperty]
    private bool _configExists;

    // Automount Settings
    [ObservableProperty]
    private bool _automountEnabled = true;

    [ObservableProperty]
    private string _automountRoot = "/mnt/";

    [ObservableProperty]
    private string? _automountOptions;

    [ObservableProperty]
    private bool _mountFsTab = true;

    // Network Settings
    [ObservableProperty]
    private bool _generateHosts = true;

    [ObservableProperty]
    private bool _generateResolvConf = true;

    [ObservableProperty]
    private string? _hostname;

    // Interop Settings
    [ObservableProperty]
    private bool _interopEnabled = true;

    [ObservableProperty]
    private bool _appendWindowsPath = true;

    // User Settings
    [ObservableProperty]
    private string? _defaultUser;

    // Boot Settings
    [ObservableProperty]
    private bool _systemdEnabled = true;

    [ObservableProperty]
    private string? _bootCommand;

    /// <summary>
    /// Gets the path to the current config file.
    /// </summary>
    public string? ConfigPath => SelectedDistribution is not null
        ? _configService.GetConfigPath(SelectedDistribution)
        : null;

    /// <summary>
    /// Initializes a new instance of the <see cref="DistroSettingsViewModel"/> class.
    /// </summary>
    public DistroSettingsViewModel(
        IWslDistroConfigService configService,
        IWslService wslService,
        IDialogService dialogService)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _wslService = wslService ?? throw new ArgumentNullException(nameof(wslService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    }

    /// <summary>
    /// Loads the list of distributions.
    /// </summary>
    [RelayCommand]
    public async Task LoadDistributionsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var distributions = await _wslService.GetDistributionsAsync();
            Distributions.Clear();
            foreach (var distro in distributions.OrderBy(d => d.Name))
            {
                Distributions.Add(distro.Name);
            }

            // Select first distribution if none selected
            if (SelectedDistribution is null && Distributions.Count > 0)
            {
                SelectedDistribution = Distributions[0];
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load distributions: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Loads the configuration for the selected distribution.
    /// </summary>
    [RelayCommand]
    public async Task LoadConfigAsync()
    {
        if (SelectedDistribution is null)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            ConfigExists = _configService.ConfigExists(SelectedDistribution);
            _originalConfig = await _configService.ReadConfigAsync(SelectedDistribution);
            ApplyConfigToViewModel(_originalConfig);
            IsDirty = false;
            OnPropertyChanged(nameof(ConfigPath));
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
        if (SelectedDistribution is null || !IsDirty)
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
            if (ConfigExists)
            {
                await _configService.CreateBackupAsync(SelectedDistribution);
            }

            // Save the configuration
            await _configService.WriteConfigAsync(SelectedDistribution, config);

            _originalConfig = config;
            IsDirty = false;
            ConfigExists = true;
            SuccessMessage = $"Settings saved. Restart {SelectedDistribution} for changes to take effect.";
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

        ApplyConfigToViewModel(new WslDistroConfig());
        IsDirty = true;
    }

    partial void OnSelectedDistributionChanged(string? value)
    {
        if (value is not null)
        {
            _ = LoadConfigAsync();
        }
    }

    private void ApplyConfigToViewModel(WslDistroConfig config)
    {
        // Automount settings
        AutomountEnabled = config.Automount.Enabled ?? true;
        AutomountRoot = config.Automount.Root ?? "/mnt/";
        AutomountOptions = config.Automount.Options;
        MountFsTab = config.Automount.MountFsTab ?? true;

        // Network settings
        GenerateHosts = config.Network.GenerateHosts ?? true;
        GenerateResolvConf = config.Network.GenerateResolvConf ?? true;
        Hostname = config.Network.Hostname;

        // Interop settings
        InteropEnabled = config.Interop.Enabled ?? true;
        AppendWindowsPath = config.Interop.AppendWindowsPath ?? true;

        // User settings
        DefaultUser = config.User.Default;

        // Boot settings
        SystemdEnabled = config.Boot.Systemd ?? true;
        BootCommand = config.Boot.Command;
    }

    private WslDistroConfig BuildConfigFromViewModel()
    {
        return new WslDistroConfig
        {
            Automount = new AutomountSettings
            {
                Enabled = AutomountEnabled,
                Root = string.IsNullOrWhiteSpace(AutomountRoot) ? null : AutomountRoot,
                Options = string.IsNullOrWhiteSpace(AutomountOptions) ? null : AutomountOptions,
                MountFsTab = MountFsTab
            },
            Network = new NetworkSettings
            {
                GenerateHosts = GenerateHosts,
                GenerateResolvConf = GenerateResolvConf,
                Hostname = string.IsNullOrWhiteSpace(Hostname) ? null : Hostname
            },
            Interop = new InteropSettings
            {
                Enabled = InteropEnabled,
                AppendWindowsPath = AppendWindowsPath
            },
            User = new UserSettings
            {
                Default = string.IsNullOrWhiteSpace(DefaultUser) ? null : DefaultUser
            },
            Boot = new BootSettings
            {
                Systemd = SystemdEnabled,
                Command = string.IsNullOrWhiteSpace(BootCommand) ? null : BootCommand
            }
        };
    }

    // Property change handlers to track dirty state
    partial void OnAutomountEnabledChanged(bool value) => MarkDirty();
    partial void OnAutomountRootChanged(string value) => MarkDirty();
    partial void OnAutomountOptionsChanged(string? value) => MarkDirty();
    partial void OnMountFsTabChanged(bool value) => MarkDirty();
    partial void OnGenerateHostsChanged(bool value) => MarkDirty();
    partial void OnGenerateResolvConfChanged(bool value) => MarkDirty();
    partial void OnHostnameChanged(string? value) => MarkDirty();
    partial void OnInteropEnabledChanged(bool value) => MarkDirty();
    partial void OnAppendWindowsPathChanged(bool value) => MarkDirty();
    partial void OnDefaultUserChanged(string? value) => MarkDirty();
    partial void OnSystemdEnabledChanged(bool value) => MarkDirty();
    partial void OnBootCommandChanged(string? value) => MarkDirty();

    private void MarkDirty()
    {
        if (!IsLoading)
        {
            IsDirty = true;
            SuccessMessage = null;
        }
    }
}
