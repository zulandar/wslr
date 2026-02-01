using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Wslr.Core.Interfaces;
using Wslr.Core.Models;

namespace Wslr.Infrastructure.Services;

/// <summary>
/// Service for managing WSL configuration templates.
/// </summary>
public sealed class ConfigurationTemplateService : IConfigurationTemplateService
{
    private readonly IWslConfigService _wslConfigService;
    private readonly IWslDistroConfigService _distroConfigService;
    private readonly ILogger<ConfigurationTemplateService> _logger;
    private readonly string _templatesPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly List<ConfigurationTemplate> _builtInTemplates;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationTemplateService"/> class.
    /// </summary>
    public ConfigurationTemplateService(
        IWslConfigService wslConfigService,
        IWslDistroConfigService distroConfigService,
        ILogger<ConfigurationTemplateService> logger)
    {
        _wslConfigService = wslConfigService ?? throw new ArgumentNullException(nameof(wslConfigService));
        _distroConfigService = distroConfigService ?? throw new ArgumentNullException(nameof(distroConfigService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _templatesPath = Path.Combine(appDataPath, "WSLR", "Templates");
        Directory.CreateDirectory(_templatesPath);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        _builtInTemplates = CreateBuiltInTemplates();
    }

    /// <inheritdoc />
    public IReadOnlyList<ConfigurationTemplate> GetBuiltInTemplates() => _builtInTemplates;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigurationTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var templates = new List<ConfigurationTemplate>(_builtInTemplates);

        // Load user templates
        if (Directory.Exists(_templatesPath))
        {
            foreach (var file in Directory.GetFiles(_templatesPath, "*.json"))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file, cancellationToken);
                    var template = JsonSerializer.Deserialize<ConfigurationTemplate>(json, _jsonOptions);
                    if (template is not null)
                    {
                        templates.Add(template);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load template from {File}", file);
                }
            }
        }

        return templates.OrderBy(t => t.IsBuiltIn ? 0 : 1).ThenBy(t => t.Name).ToList();
    }

    /// <inheritdoc />
    public async Task<ConfigurationTemplate?> GetTemplateAsync(string templateId, CancellationToken cancellationToken = default)
    {
        // Check built-in templates first
        var builtIn = _builtInTemplates.FirstOrDefault(t => t.Id == templateId);
        if (builtIn is not null)
        {
            return builtIn;
        }

        // Check user templates
        var filePath = GetTemplateFilePath(templateId);
        if (File.Exists(filePath))
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            return JsonSerializer.Deserialize<ConfigurationTemplate>(json, _jsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<ConfigurationTemplate> CreateTemplateAsync(ConfigurationTemplate template, CancellationToken cancellationToken = default)
    {
        if (template.IsBuiltIn)
        {
            throw new InvalidOperationException("Cannot create a built-in template.");
        }

        var newTemplate = template with
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            IsBuiltIn = false
        };

        await SaveTemplateAsync(newTemplate, cancellationToken);
        _logger.LogInformation("Created template: {Name} ({Id})", newTemplate.Name, newTemplate.Id);

        return newTemplate;
    }

    /// <inheritdoc />
    public async Task<ConfigurationTemplate> CreateTemplateFromDistributionAsync(
        string name,
        string? description,
        string distributionName,
        bool includeGlobalSettings = false,
        CancellationToken cancellationToken = default)
    {
        WslConfig? globalSettings = null;
        if (includeGlobalSettings)
        {
            globalSettings = await _wslConfigService.ReadConfigAsync(cancellationToken);
        }

        var distroSettings = await _distroConfigService.ReadConfigAsync(distributionName, cancellationToken);

        var template = new ConfigurationTemplate
        {
            Name = name,
            Description = description ?? $"Created from {distributionName}",
            GlobalSettings = globalSettings,
            DistroSettings = distroSettings
        };

        return await CreateTemplateAsync(template, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConfigurationTemplate> UpdateTemplateAsync(ConfigurationTemplate template, CancellationToken cancellationToken = default)
    {
        if (template.IsBuiltIn || _builtInTemplates.Any(t => t.Id == template.Id))
        {
            throw new InvalidOperationException("Cannot modify a built-in template.");
        }

        var updatedTemplate = template with
        {
            ModifiedAt = DateTime.UtcNow
        };

        await SaveTemplateAsync(updatedTemplate, cancellationToken);
        _logger.LogInformation("Updated template: {Name} ({Id})", updatedTemplate.Name, updatedTemplate.Id);

        return updatedTemplate;
    }

    /// <inheritdoc />
    public Task<bool> DeleteTemplateAsync(string templateId, CancellationToken cancellationToken = default)
    {
        if (_builtInTemplates.Any(t => t.Id == templateId))
        {
            throw new InvalidOperationException("Cannot delete a built-in template.");
        }

        var filePath = GetTemplateFilePath(templateId);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("Deleted template: {Id}", templateId);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public async Task<ConfigurationTemplate> DuplicateTemplateAsync(string templateId, string? newName = null, CancellationToken cancellationToken = default)
    {
        var original = await GetTemplateAsync(templateId, cancellationToken)
            ?? throw new InvalidOperationException($"Template '{templateId}' not found.");

        var duplicate = original with
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            Name = newName ?? $"{original.Name} (Copy)",
            IsBuiltIn = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        await SaveTemplateAsync(duplicate, cancellationToken);
        _logger.LogInformation("Duplicated template: {OriginalId} -> {NewId}", templateId, duplicate.Id);

        return duplicate;
    }

    /// <inheritdoc />
    public async Task<TemplateApplyResult> ApplyTemplateAsync(
        string templateId,
        string distributionName,
        TemplateApplyOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new TemplateApplyOptions();

        var template = await GetTemplateAsync(templateId, cancellationToken);
        if (template is null)
        {
            return TemplateApplyResult.Failed($"Template '{templateId}' not found.");
        }

        try
        {
            var globalApplied = false;
            var distroApplied = false;

            // Apply global settings
            if (options.ApplyGlobalSettings && template.GlobalSettings is not null)
            {
                var currentGlobal = await _wslConfigService.ReadConfigAsync(cancellationToken);
                var merged = options.MergeMode == TemplateMergeMode.Merge
                    ? MergeGlobalSettings(currentGlobal, template.GlobalSettings)
                    : template.GlobalSettings;

                await _wslConfigService.CreateBackupAsync(cancellationToken);
                await _wslConfigService.WriteConfigAsync(merged, cancellationToken);
                globalApplied = true;
                _logger.LogInformation("Applied global settings from template {TemplateId}", templateId);
            }

            // Apply distro settings
            if (options.ApplyDistroSettings && template.DistroSettings is not null)
            {
                var currentDistro = await _distroConfigService.ReadConfigAsync(distributionName, cancellationToken);
                var merged = options.MergeMode == TemplateMergeMode.Merge
                    ? MergeDistroSettings(currentDistro, template.DistroSettings)
                    : template.DistroSettings;

                if (_distroConfigService.ConfigExists(distributionName))
                {
                    await _distroConfigService.CreateBackupAsync(distributionName, cancellationToken);
                }
                await _distroConfigService.WriteConfigAsync(distributionName, merged, cancellationToken);
                distroApplied = true;
                _logger.LogInformation("Applied distro settings from template {TemplateId} to {Distro}", templateId, distributionName);
            }

            return TemplateApplyResult.Succeeded(globalApplied, distroApplied);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply template {TemplateId} to {Distro}", templateId, distributionName);
            return TemplateApplyResult.Failed(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, TemplateApplyResult>> ApplyTemplateToMultipleAsync(
        string templateId,
        IEnumerable<string> distributionNames,
        TemplateApplyOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, TemplateApplyResult>();

        foreach (var distro in distributionNames)
        {
            results[distro] = await ApplyTemplateAsync(templateId, distro, options, cancellationToken);
        }

        return results;
    }

    /// <inheritdoc />
    public async Task ExportTemplateAsync(string templateId, string filePath, CancellationToken cancellationToken = default)
    {
        var template = await GetTemplateAsync(templateId, cancellationToken)
            ?? throw new InvalidOperationException($"Template '{templateId}' not found.");

        // Export as non-built-in
        var exportTemplate = template with { IsBuiltIn = false };

        var json = JsonSerializer.Serialize(exportTemplate, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
        _logger.LogInformation("Exported template {TemplateId} to {Path}", templateId, filePath);
    }

    /// <inheritdoc />
    public async Task<ConfigurationTemplate> ImportTemplateAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var template = JsonSerializer.Deserialize<ConfigurationTemplate>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to parse template file.");

        // Always import as non-built-in with a new ID
        var importedTemplate = template with
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            IsBuiltIn = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        await SaveTemplateAsync(importedTemplate, cancellationToken);
        _logger.LogInformation("Imported template from {Path} as {Id}", filePath, importedTemplate.Id);

        return importedTemplate;
    }

    /// <inheritdoc />
    public async Task<TemplatePreviewResult> PreviewTemplateAsync(
        string templateId,
        string distributionName,
        TemplateApplyOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new TemplateApplyOptions();

        var template = await GetTemplateAsync(templateId, cancellationToken);
        if (template is null)
        {
            return new TemplatePreviewResult();
        }

        var globalChanges = new List<SettingChange>();
        var distroChanges = new List<SettingChange>();

        // Preview global changes
        if (options.ApplyGlobalSettings && template.GlobalSettings is not null)
        {
            var current = await _wslConfigService.ReadConfigAsync(cancellationToken);
            globalChanges.AddRange(CompareGlobalSettings(current, template.GlobalSettings));
        }

        // Preview distro changes
        if (options.ApplyDistroSettings && template.DistroSettings is not null)
        {
            var current = await _distroConfigService.ReadConfigAsync(distributionName, cancellationToken);
            distroChanges.AddRange(CompareDistroSettings(current, template.DistroSettings));
        }

        return new TemplatePreviewResult
        {
            GlobalChanges = globalChanges,
            DistroChanges = distroChanges
        };
    }

    private string GetTemplateFilePath(string templateId) =>
        Path.Combine(_templatesPath, $"{templateId}.json");

    private async Task SaveTemplateAsync(ConfigurationTemplate template, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(template, _jsonOptions);
        var filePath = GetTemplateFilePath(template.Id);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    private static WslConfig MergeGlobalSettings(WslConfig current, WslConfig template)
    {
        return new WslConfig
        {
            Wsl2 = new Wsl2Settings
            {
                Memory = template.Wsl2.Memory ?? current.Wsl2.Memory,
                Processors = template.Wsl2.Processors ?? current.Wsl2.Processors,
                Swap = template.Wsl2.Swap ?? current.Wsl2.Swap,
                SwapFile = template.Wsl2.SwapFile ?? current.Wsl2.SwapFile,
                LocalhostForwarding = template.Wsl2.LocalhostForwarding ?? current.Wsl2.LocalhostForwarding,
                GuiApplications = template.Wsl2.GuiApplications ?? current.Wsl2.GuiApplications,
                DebugConsole = template.Wsl2.DebugConsole ?? current.Wsl2.DebugConsole,
                NestedVirtualization = template.Wsl2.NestedVirtualization ?? current.Wsl2.NestedVirtualization,
                VmIdleTimeout = template.Wsl2.VmIdleTimeout ?? current.Wsl2.VmIdleTimeout,
                Kernel = template.Wsl2.Kernel ?? current.Wsl2.Kernel,
                KernelCommandLine = template.Wsl2.KernelCommandLine ?? current.Wsl2.KernelCommandLine,
                PageReporting = template.Wsl2.PageReporting ?? current.Wsl2.PageReporting,
                DnsTunneling = template.Wsl2.DnsTunneling ?? current.Wsl2.DnsTunneling,
                Firewall = template.Wsl2.Firewall ?? current.Wsl2.Firewall,
                NetworkingMode = template.Wsl2.NetworkingMode ?? current.Wsl2.NetworkingMode
            },
            Experimental = new ExperimentalSettings
            {
                AutoMemoryReclaim = template.Experimental.AutoMemoryReclaim ?? current.Experimental.AutoMemoryReclaim,
                SparseVhd = template.Experimental.SparseVhd ?? current.Experimental.SparseVhd,
                UseWindowsDnsCache = template.Experimental.UseWindowsDnsCache ?? current.Experimental.UseWindowsDnsCache,
                BestEffortDnsParsing = template.Experimental.BestEffortDnsParsing ?? current.Experimental.BestEffortDnsParsing
            }
        };
    }

    private static WslDistroConfig MergeDistroSettings(WslDistroConfig current, WslDistroConfig template)
    {
        return new WslDistroConfig
        {
            Automount = new AutomountSettings
            {
                Enabled = template.Automount.Enabled ?? current.Automount.Enabled,
                Root = template.Automount.Root ?? current.Automount.Root,
                Options = template.Automount.Options ?? current.Automount.Options,
                MountFsTab = template.Automount.MountFsTab ?? current.Automount.MountFsTab
            },
            Network = new NetworkSettings
            {
                GenerateHosts = template.Network.GenerateHosts ?? current.Network.GenerateHosts,
                GenerateResolvConf = template.Network.GenerateResolvConf ?? current.Network.GenerateResolvConf,
                Hostname = template.Network.Hostname ?? current.Network.Hostname
            },
            Interop = new InteropSettings
            {
                Enabled = template.Interop.Enabled ?? current.Interop.Enabled,
                AppendWindowsPath = template.Interop.AppendWindowsPath ?? current.Interop.AppendWindowsPath
            },
            User = new UserSettings
            {
                Default = template.User.Default ?? current.User.Default
            },
            Boot = new BootSettings
            {
                Systemd = template.Boot.Systemd ?? current.Boot.Systemd,
                Command = template.Boot.Command ?? current.Boot.Command
            }
        };
    }

    private static IEnumerable<SettingChange> CompareGlobalSettings(WslConfig current, WslConfig template)
    {
        var changes = new List<SettingChange>();

        // Compare WSL2 settings
        CompareValue(changes, "wsl2", "memory", current.Wsl2.Memory, template.Wsl2.Memory);
        CompareValue(changes, "wsl2", "processors", current.Wsl2.Processors?.ToString(), template.Wsl2.Processors?.ToString());
        CompareValue(changes, "wsl2", "swap", current.Wsl2.Swap, template.Wsl2.Swap);
        CompareValue(changes, "wsl2", "localhostForwarding", current.Wsl2.LocalhostForwarding?.ToString(), template.Wsl2.LocalhostForwarding?.ToString());
        CompareValue(changes, "wsl2", "guiApplications", current.Wsl2.GuiApplications?.ToString(), template.Wsl2.GuiApplications?.ToString());
        CompareValue(changes, "wsl2", "nestedVirtualization", current.Wsl2.NestedVirtualization?.ToString(), template.Wsl2.NestedVirtualization?.ToString());
        CompareValue(changes, "wsl2", "networkingMode", current.Wsl2.NetworkingMode, template.Wsl2.NetworkingMode);

        // Compare Experimental settings
        CompareValue(changes, "experimental", "autoMemoryReclaim", current.Experimental.AutoMemoryReclaim, template.Experimental.AutoMemoryReclaim);
        CompareValue(changes, "experimental", "sparseVhd", current.Experimental.SparseVhd?.ToString(), template.Experimental.SparseVhd?.ToString());

        return changes;
    }

    private static IEnumerable<SettingChange> CompareDistroSettings(WslDistroConfig current, WslDistroConfig template)
    {
        var changes = new List<SettingChange>();

        // Compare Automount
        CompareValue(changes, "automount", "enabled", current.Automount.Enabled?.ToString(), template.Automount.Enabled?.ToString());
        CompareValue(changes, "automount", "root", current.Automount.Root, template.Automount.Root);
        CompareValue(changes, "automount", "options", current.Automount.Options, template.Automount.Options);
        CompareValue(changes, "automount", "mountFsTab", current.Automount.MountFsTab?.ToString(), template.Automount.MountFsTab?.ToString());

        // Compare Network
        CompareValue(changes, "network", "generateHosts", current.Network.GenerateHosts?.ToString(), template.Network.GenerateHosts?.ToString());
        CompareValue(changes, "network", "generateResolvConf", current.Network.GenerateResolvConf?.ToString(), template.Network.GenerateResolvConf?.ToString());
        CompareValue(changes, "network", "hostname", current.Network.Hostname, template.Network.Hostname);

        // Compare Interop
        CompareValue(changes, "interop", "enabled", current.Interop.Enabled?.ToString(), template.Interop.Enabled?.ToString());
        CompareValue(changes, "interop", "appendWindowsPath", current.Interop.AppendWindowsPath?.ToString(), template.Interop.AppendWindowsPath?.ToString());

        // Compare User
        CompareValue(changes, "user", "default", current.User.Default, template.User.Default);

        // Compare Boot
        CompareValue(changes, "boot", "systemd", current.Boot.Systemd?.ToString(), template.Boot.Systemd?.ToString());
        CompareValue(changes, "boot", "command", current.Boot.Command, template.Boot.Command);

        return changes;
    }

    private static void CompareValue(List<SettingChange> changes, string section, string setting, string? current, string? template)
    {
        if (template is null)
        {
            return; // Template doesn't set this value
        }

        var changeType = current is null ? SettingChangeType.Add
            : string.Equals(current, template, StringComparison.OrdinalIgnoreCase) ? SettingChangeType.Unchanged
            : SettingChangeType.Modify;

        if (changeType != SettingChangeType.Unchanged)
        {
            changes.Add(new SettingChange
            {
                Section = section,
                Setting = setting,
                CurrentValue = current,
                NewValue = template,
                ChangeType = changeType
            });
        }
    }

    private static List<ConfigurationTemplate> CreateBuiltInTemplates()
    {
        return
        [
            new ConfigurationTemplate
            {
                Id = "builtin-dev",
                Name = "Development",
                Description = "Full interop, systemd, Windows PATH - ideal for development workflows",
                IsBuiltIn = true,
                DistroSettings = new WslDistroConfig
                {
                    Automount = new AutomountSettings
                    {
                        Enabled = true,
                        Root = "/mnt/",
                        Options = "metadata,umask=22,fmask=11",
                        MountFsTab = true
                    },
                    Network = new NetworkSettings
                    {
                        GenerateHosts = true,
                        GenerateResolvConf = true
                    },
                    Interop = new InteropSettings
                    {
                        Enabled = true,
                        AppendWindowsPath = true
                    },
                    Boot = new BootSettings
                    {
                        Systemd = true
                    }
                }
            },
            new ConfigurationTemplate
            {
                Id = "builtin-server",
                Name = "Server",
                Description = "Minimal configuration for server workloads - no GUI, basic mounts",
                IsBuiltIn = true,
                DistroSettings = new WslDistroConfig
                {
                    Automount = new AutomountSettings
                    {
                        Enabled = true,
                        Root = "/mnt/",
                        MountFsTab = false
                    },
                    Network = new NetworkSettings
                    {
                        GenerateHosts = true,
                        GenerateResolvConf = true
                    },
                    Interop = new InteropSettings
                    {
                        Enabled = false,
                        AppendWindowsPath = false
                    },
                    Boot = new BootSettings
                    {
                        Systemd = true
                    }
                },
                GlobalSettings = new WslConfig
                {
                    Wsl2 = new Wsl2Settings
                    {
                        GuiApplications = false
                    }
                }
            },
            new ConfigurationTemplate
            {
                Id = "builtin-isolated",
                Name = "Isolated",
                Description = "Maximum isolation - no Windows interop or PATH, custom hostname",
                IsBuiltIn = true,
                DistroSettings = new WslDistroConfig
                {
                    Automount = new AutomountSettings
                    {
                        Enabled = false
                    },
                    Network = new NetworkSettings
                    {
                        GenerateHosts = false,
                        GenerateResolvConf = false
                    },
                    Interop = new InteropSettings
                    {
                        Enabled = false,
                        AppendWindowsPath = false
                    },
                    Boot = new BootSettings
                    {
                        Systemd = true
                    }
                }
            },
            new ConfigurationTemplate
            {
                Id = "builtin-lowmem",
                Name = "Low Memory",
                Description = "Reduced memory and swap limits for constrained systems",
                IsBuiltIn = true,
                GlobalSettings = new WslConfig
                {
                    Wsl2 = new Wsl2Settings
                    {
                        Memory = "4GB",
                        Swap = "2GB",
                        PageReporting = true
                    },
                    Experimental = new ExperimentalSettings
                    {
                        AutoMemoryReclaim = "gradual",
                        SparseVhd = true
                    }
                },
                DistroSettings = new WslDistroConfig
                {
                    Boot = new BootSettings
                    {
                        Systemd = true
                    }
                }
            }
        ];
    }
}
