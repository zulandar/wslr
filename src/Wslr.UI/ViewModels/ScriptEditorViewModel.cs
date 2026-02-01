using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Wslr.Core.Interfaces;
using Wslr.Core.Models;

namespace Wslr.UI.ViewModels;

/// <summary>
/// ViewModel for the script editor view.
/// </summary>
public partial class ScriptEditorViewModel : ObservableObject
{
    private readonly IScriptExecutionService _scriptExecutionService;
    private readonly IScriptTemplateService _scriptTemplateService;
    private readonly IWslService _wslService;
    private readonly ILogger<ScriptEditorViewModel> _logger;

    private CancellationTokenSource? _executionCts;

    [ObservableProperty]
    private string _scriptName = "Untitled Script";

    [ObservableProperty]
    private string _scriptDescription = string.Empty;

    [ObservableProperty]
    private string _scriptContent = "#!/bin/bash\nset -e\n\n# Your script here\necho \"Hello from WSL!\"\n";

    [ObservableProperty]
    private string _outputContent = string.Empty;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isValidating;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private bool _isModified;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _successMessage;

    [ObservableProperty]
    private string? _validationMessage;

    [ObservableProperty]
    private bool _isValidationSuccess;

    [ObservableProperty]
    private string? _selectedDistribution;

    [ObservableProperty]
    private ScriptTemplate? _currentTemplate;

    [ObservableProperty]
    private int _lastExitCode;

    /// <summary>
    /// Available WSL distributions for script execution.
    /// </summary>
    public ObservableCollection<string> AvailableDistributions { get; } = [];

    /// <summary>
    /// Available script templates.
    /// </summary>
    public ObservableCollection<ScriptTemplate> AvailableTemplates { get; } = [];

    public ScriptEditorViewModel(
        IScriptExecutionService scriptExecutionService,
        IScriptTemplateService scriptTemplateService,
        IWslService wslService,
        ILogger<ScriptEditorViewModel> logger)
    {
        _scriptExecutionService = scriptExecutionService ?? throw new ArgumentNullException(nameof(scriptExecutionService));
        _scriptTemplateService = scriptTemplateService ?? throw new ArgumentNullException(nameof(scriptTemplateService));
        _wslService = wslService ?? throw new ArgumentNullException(nameof(wslService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Loads available distributions and templates.
    /// </summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            // Load distributions
            var distributions = await _wslService.GetDistributionsAsync();
            AvailableDistributions.Clear();
            foreach (var distro in distributions.Where(d => d.State == DistributionState.Running || d.State == DistributionState.Stopped))
            {
                AvailableDistributions.Add(distro.Name);
            }

            if (AvailableDistributions.Count > 0 && SelectedDistribution == null)
            {
                SelectedDistribution = AvailableDistributions[0];
            }

            // Load templates
            var templates = await _scriptTemplateService.GetAllTemplatesAsync();
            AvailableTemplates.Clear();
            foreach (var template in templates)
            {
                AvailableTemplates.Add(template);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load distributions and templates");
            ErrorMessage = $"Failed to load: {ex.Message}";
        }
    }

    /// <summary>
    /// Runs the current script in the selected distribution.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRunScript))]
    public async Task RunScriptAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedDistribution))
        {
            ErrorMessage = "Please select a distribution first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(ScriptContent))
        {
            ErrorMessage = "Script content is empty.";
            return;
        }

        IsRunning = true;
        ErrorMessage = null;
        SuccessMessage = null;
        OutputContent = string.Empty;

        var outputBuilder = new StringBuilder();
        _executionCts = new CancellationTokenSource();

        try
        {
            _logger.LogInformation("Executing script in {Distribution}", SelectedDistribution);

            var progress = new Progress<string>(line =>
            {
                outputBuilder.AppendLine(line);
                OutputContent = outputBuilder.ToString();
            });

            var result = await _scriptExecutionService.ExecuteScriptAsync(
                SelectedDistribution,
                ScriptContent,
                progress: progress,
                cancellationToken: _executionCts.Token);

            LastExitCode = result.ExitCode;

            if (result.WasCancelled)
            {
                ErrorMessage = "Script execution was cancelled.";
            }
            else if (result.IsSuccess)
            {
                SuccessMessage = $"Script completed successfully in {result.Duration.TotalSeconds:F1}s";
            }
            else
            {
                ErrorMessage = $"Script failed with exit code {result.ExitCode}";
                if (!string.IsNullOrWhiteSpace(result.StandardError))
                {
                    outputBuilder.AppendLine($"\n[stderr]\n{result.StandardError}");
                    OutputContent = outputBuilder.ToString();
                }
            }
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Script execution was cancelled.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute script");
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
            _executionCts?.Dispose();
            _executionCts = null;
        }
    }

    private bool CanRunScript() => !IsRunning && !string.IsNullOrWhiteSpace(SelectedDistribution);

    /// <summary>
    /// Cancels the currently running script.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCancelScript))]
    public void CancelScript()
    {
        _executionCts?.Cancel();
    }

    private bool CanCancelScript() => IsRunning;

    /// <summary>
    /// Validates the current script syntax.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanValidateScript))]
    public async Task ValidateScriptAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedDistribution))
        {
            ErrorMessage = "Please select a distribution first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(ScriptContent))
        {
            ErrorMessage = "Script content is empty.";
            return;
        }

        IsValidating = true;
        ValidationMessage = null;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var result = await _scriptExecutionService.ValidateScriptAsync(
                SelectedDistribution,
                ScriptContent);

            IsValidationSuccess = result.IsValid;

            if (result.IsValid)
            {
                ValidationMessage = "Script syntax is valid.";
                SuccessMessage = "Validation passed!";
            }
            else
            {
                var lineInfo = result.ErrorLine.HasValue ? $" (line {result.ErrorLine})" : "";
                ValidationMessage = $"Syntax error{lineInfo}: {result.ErrorMessage}";
                ErrorMessage = ValidationMessage;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate script");
            ErrorMessage = $"Validation failed: {ex.Message}";
            IsValidationSuccess = false;
        }
        finally
        {
            IsValidating = false;
        }
    }

    private bool CanValidateScript() => !IsValidating && !IsRunning && !string.IsNullOrWhiteSpace(SelectedDistribution);

    /// <summary>
    /// Saves the current script as a template.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSaveScript))]
    public async Task SaveScriptAsync()
    {
        if (string.IsNullOrWhiteSpace(ScriptName))
        {
            ErrorMessage = "Please enter a script name.";
            return;
        }

        if (string.IsNullOrWhiteSpace(ScriptContent))
        {
            ErrorMessage = "Script content is empty.";
            return;
        }

        IsSaving = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var template = new ScriptTemplate
            {
                Name = ScriptName,
                Description = ScriptDescription,
                ScriptContent = ScriptContent,
                Category = "User Scripts"
            };

            if (CurrentTemplate != null && !CurrentTemplate.IsBuiltIn)
            {
                // Update existing template
                template = template with { Id = CurrentTemplate.Id };
                await _scriptTemplateService.UpdateTemplateAsync(template);
                SuccessMessage = "Template updated successfully.";
            }
            else
            {
                // Create new template
                var created = await _scriptTemplateService.CreateTemplateAsync(template);
                CurrentTemplate = created;
                SuccessMessage = "Template saved successfully.";
            }

            IsModified = false;

            // Refresh templates list
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save script");
            ErrorMessage = $"Failed to save: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool CanSaveScript() => !IsSaving && !string.IsNullOrWhiteSpace(ScriptName);

    /// <summary>
    /// Loads a template into the editor.
    /// </summary>
    [RelayCommand]
    public void LoadTemplate(ScriptTemplate? template)
    {
        if (template == null) return;

        CurrentTemplate = template;
        ScriptName = template.Name;
        ScriptDescription = template.Description ?? string.Empty;
        ScriptContent = template.ScriptContent;
        IsModified = false;

        SuccessMessage = $"Loaded template: {template.Name}";
        ErrorMessage = null;
    }

    /// <summary>
    /// Creates a new empty script.
    /// </summary>
    [RelayCommand]
    public void NewScript()
    {
        CurrentTemplate = null;
        ScriptName = "Untitled Script";
        ScriptDescription = string.Empty;
        ScriptContent = "#!/bin/bash\nset -e\n\n# Your script here\n";
        OutputContent = string.Empty;
        IsModified = false;
        ErrorMessage = null;
        SuccessMessage = null;
        ValidationMessage = null;
    }

    /// <summary>
    /// Clears the output console.
    /// </summary>
    [RelayCommand]
    public void ClearOutput()
    {
        OutputContent = string.Empty;
        ErrorMessage = null;
        SuccessMessage = null;
    }

    partial void OnScriptContentChanged(string value)
    {
        IsModified = true;
        // Clear validation when content changes
        ValidationMessage = null;
    }

    partial void OnScriptNameChanged(string value)
    {
        IsModified = true;
    }

    partial void OnScriptDescriptionChanged(string value)
    {
        IsModified = true;
    }

    partial void OnIsRunningChanged(bool value)
    {
        RunScriptCommand.NotifyCanExecuteChanged();
        CancelScriptCommand.NotifyCanExecuteChanged();
        ValidateScriptCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsValidatingChanged(bool value)
    {
        ValidateScriptCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsSavingChanged(bool value)
    {
        SaveScriptCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedDistributionChanged(string? value)
    {
        RunScriptCommand.NotifyCanExecuteChanged();
        ValidateScriptCommand.NotifyCanExecuteChanged();
    }
}
