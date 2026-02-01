using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Wslr.Core.Interfaces;
using Wslr.Core.Models;
using Wslr.UI.Services;

namespace Wslr.UI.ViewModels;

/// <summary>
/// ViewModel for the template list and management view.
/// </summary>
public partial class TemplateListViewModel : ObservableObject
{
    private readonly IConfigurationTemplateService _templateService;
    private readonly IWslService _wslService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<TemplateListViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<TemplateItemViewModel> _templates = [];

    [ObservableProperty]
    private TemplateItemViewModel? _selectedTemplate;

    [ObservableProperty]
    private ObservableCollection<string> _distributions = [];

    [ObservableProperty]
    private string? _selectedDistribution;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _successMessage;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string _editDescription = string.Empty;

    [ObservableProperty]
    private bool _applyGlobalSettings = true;

    [ObservableProperty]
    private bool _applyDistroSettings = true;

    [ObservableProperty]
    private bool _useMergeMode = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateListViewModel"/> class.
    /// </summary>
    public TemplateListViewModel(
        IConfigurationTemplateService templateService,
        IWslService wslService,
        IDialogService dialogService,
        ILogger<TemplateListViewModel> logger)
    {
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        _wslService = wslService ?? throw new ArgumentNullException(nameof(wslService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Loads all templates and distributions.
    /// </summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            // Load templates
            var templates = await _templateService.GetAllTemplatesAsync();
            Templates.Clear();
            foreach (var template in templates)
            {
                Templates.Add(TemplateItemViewModel.FromModel(template));
            }

            // Load distributions
            var distributions = await _wslService.GetDistributionsAsync();
            Distributions.Clear();
            foreach (var distro in distributions.OrderBy(d => d.Name))
            {
                Distributions.Add(distro.Name);
            }

            if (Distributions.Count > 0 && SelectedDistribution is null)
            {
                SelectedDistribution = Distributions[0];
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load templates");
            ErrorMessage = $"Failed to load templates: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Creates a new template.
    /// </summary>
    [RelayCommand]
    public void NewTemplate()
    {
        IsEditing = true;
        EditName = "New Template";
        EditDescription = string.Empty;
        SelectedTemplate = null;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    /// <summary>
    /// Creates a template from the selected distribution's current configuration.
    /// </summary>
    [RelayCommand]
    public async Task CreateFromDistributionAsync()
    {
        if (SelectedDistribution is null)
        {
            ErrorMessage = "Please select a distribution first.";
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Create Template from Distribution",
            $"Create a new template from the current configuration of '{SelectedDistribution}'?");

        if (!confirmed)
        {
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            var template = await _templateService.CreateTemplateFromDistributionAsync(
                $"From {SelectedDistribution}",
                $"Configuration captured from {SelectedDistribution}",
                SelectedDistribution,
                includeGlobalSettings: true);

            Templates.Add(TemplateItemViewModel.FromModel(template));
            SuccessMessage = $"Template created from {SelectedDistribution}";
            _logger.LogInformation("Created template from distribution {Distro}", SelectedDistribution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create template from distribution");
            ErrorMessage = $"Failed to create template: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// Saves the currently edited template.
    /// </summary>
    [RelayCommand]
    public async Task SaveTemplateAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            ErrorMessage = "Template name is required.";
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            ConfigurationTemplate template;
            if (SelectedTemplate is not null && !SelectedTemplate.IsBuiltIn)
            {
                // Update existing template
                var existing = await _templateService.GetTemplateAsync(SelectedTemplate.Id);
                if (existing is not null)
                {
                    template = await _templateService.UpdateTemplateAsync(existing with
                    {
                        Name = EditName,
                        Description = EditDescription
                    });

                    SelectedTemplate.Name = template.Name;
                    SelectedTemplate.Description = template.Description;
                }
            }
            else
            {
                // Create new template
                template = await _templateService.CreateTemplateAsync(new ConfigurationTemplate
                {
                    Name = EditName,
                    Description = EditDescription
                });

                Templates.Add(TemplateItemViewModel.FromModel(template));
            }

            IsEditing = false;
            SuccessMessage = "Template saved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save template");
            ErrorMessage = $"Failed to save template: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// Cancels editing.
    /// </summary>
    [RelayCommand]
    public void CancelEdit()
    {
        IsEditing = false;
        EditName = string.Empty;
        EditDescription = string.Empty;
    }

    /// <summary>
    /// Edits the selected template.
    /// </summary>
    [RelayCommand]
    public void EditTemplate()
    {
        if (SelectedTemplate is null)
        {
            return;
        }

        if (SelectedTemplate.IsBuiltIn)
        {
            ErrorMessage = "Built-in templates cannot be edited. Duplicate it first.";
            return;
        }

        IsEditing = true;
        EditName = SelectedTemplate.Name;
        EditDescription = SelectedTemplate.Description ?? string.Empty;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    /// <summary>
    /// Duplicates the selected template.
    /// </summary>
    [RelayCommand]
    public async Task DuplicateTemplateAsync()
    {
        if (SelectedTemplate is null)
        {
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            var duplicate = await _templateService.DuplicateTemplateAsync(SelectedTemplate.Id);
            Templates.Add(TemplateItemViewModel.FromModel(duplicate));
            SuccessMessage = $"Template duplicated as '{duplicate.Name}'";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to duplicate template");
            ErrorMessage = $"Failed to duplicate template: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// Deletes the selected template.
    /// </summary>
    [RelayCommand]
    public async Task DeleteTemplateAsync()
    {
        if (SelectedTemplate is null)
        {
            return;
        }

        if (SelectedTemplate.IsBuiltIn)
        {
            ErrorMessage = "Built-in templates cannot be deleted.";
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Template",
            $"Are you sure you want to delete the template '{SelectedTemplate.Name}'?");

        if (!confirmed)
        {
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            await _templateService.DeleteTemplateAsync(SelectedTemplate.Id);
            Templates.Remove(SelectedTemplate);
            SelectedTemplate = null;
            SuccessMessage = "Template deleted.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete template");
            ErrorMessage = $"Failed to delete template: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// Applies the selected template to the selected distribution.
    /// </summary>
    [RelayCommand]
    public async Task ApplyTemplateAsync()
    {
        if (SelectedTemplate is null)
        {
            ErrorMessage = "Please select a template.";
            return;
        }

        if (SelectedDistribution is null)
        {
            ErrorMessage = "Please select a distribution.";
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Apply Template",
            $"Apply template '{SelectedTemplate.Name}' to '{SelectedDistribution}'?\n\n" +
            $"Global settings: {(ApplyGlobalSettings ? "Yes" : "No")}\n" +
            $"Distribution settings: {(ApplyDistroSettings ? "Yes" : "No")}\n" +
            $"Mode: {(UseMergeMode ? "Merge" : "Overwrite")}");

        if (!confirmed)
        {
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            var options = new TemplateApplyOptions
            {
                ApplyGlobalSettings = ApplyGlobalSettings,
                ApplyDistroSettings = ApplyDistroSettings,
                MergeMode = UseMergeMode ? TemplateMergeMode.Merge : TemplateMergeMode.Overwrite
            };

            var result = await _templateService.ApplyTemplateAsync(SelectedTemplate.Id, SelectedDistribution, options);

            if (result.Success)
            {
                var appliedParts = new List<string>();
                if (result.GlobalSettingsApplied) appliedParts.Add("global settings");
                if (result.DistroSettingsApplied) appliedParts.Add("distribution settings");

                SuccessMessage = $"Applied {string.Join(" and ", appliedParts)}. " +
                    (result.RestartRequired ? "Restart WSL for changes to take effect." : "");
            }
            else
            {
                ErrorMessage = $"Failed to apply template: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply template");
            ErrorMessage = $"Failed to apply template: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// Exports the selected template to a file.
    /// </summary>
    [RelayCommand]
    public async Task ExportTemplateAsync()
    {
        if (SelectedTemplate is null)
        {
            return;
        }

        var fileName = $"{SelectedTemplate.Name.Replace(' ', '-')}.json";
        var filePath = await _dialogService.ShowSaveFileDialogAsync(
            "Export Template",
            fileName,
            "JSON files|*.json|All files|*.*");

        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            await _templateService.ExportTemplateAsync(SelectedTemplate.Id, filePath);
            SuccessMessage = $"Template exported to {filePath}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export template");
            ErrorMessage = $"Failed to export template: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// Imports a template from a file.
    /// </summary>
    [RelayCommand]
    public async Task ImportTemplateAsync()
    {
        var filePath = await _dialogService.ShowOpenFileDialogAsync(
            "Import Template",
            "JSON files|*.json|All files|*.*");

        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            var template = await _templateService.ImportTemplateAsync(filePath);
            Templates.Add(TemplateItemViewModel.FromModel(template));
            SuccessMessage = $"Template '{template.Name}' imported successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import template");
            ErrorMessage = $"Failed to import template: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    partial void OnSelectedTemplateChanged(TemplateItemViewModel? value)
    {
        if (value is not null && IsEditing)
        {
            CancelEdit();
        }
    }
}

/// <summary>
/// ViewModel for a single template item.
/// </summary>
public partial class TemplateItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private bool _isBuiltIn;

    [ObservableProperty]
    private bool _hasGlobalSettings;

    [ObservableProperty]
    private bool _hasDistroSettings;

    [ObservableProperty]
    private DateTime _modifiedAt;

    /// <summary>
    /// Creates a view model from a template model.
    /// </summary>
    public static TemplateItemViewModel FromModel(ConfigurationTemplate template)
    {
        return new TemplateItemViewModel
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            IsBuiltIn = template.IsBuiltIn,
            HasGlobalSettings = template.GlobalSettings is not null,
            HasDistroSettings = template.DistroSettings is not null,
            ModifiedAt = template.ModifiedAt
        };
    }
}
