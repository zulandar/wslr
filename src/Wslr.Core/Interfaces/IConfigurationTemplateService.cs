using Wslr.Core.Models;

namespace Wslr.Core.Interfaces;

/// <summary>
/// Provides operations for managing WSL configuration templates.
/// </summary>
public interface IConfigurationTemplateService
{
    /// <summary>
    /// Gets all available templates (built-in and user).
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of all templates.</returns>
    Task<IReadOnlyList<ConfigurationTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a template by its ID.
    /// </summary>
    /// <param name="templateId">The template ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The template, or null if not found.</returns>
    Task<ConfigurationTemplate?> GetTemplateAsync(string templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all built-in templates.
    /// </summary>
    /// <returns>A list of built-in templates.</returns>
    IReadOnlyList<ConfigurationTemplate> GetBuiltInTemplates();

    /// <summary>
    /// Creates a new template.
    /// </summary>
    /// <param name="template">The template to create.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The created template with assigned ID.</returns>
    Task<ConfigurationTemplate> CreateTemplateAsync(ConfigurationTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a template from the current configuration of a distribution.
    /// </summary>
    /// <param name="name">The template name.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="distributionName">The distribution to capture settings from.</param>
    /// <param name="includeGlobalSettings">Whether to include global .wslconfig settings.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The created template.</returns>
    Task<ConfigurationTemplate> CreateTemplateFromDistributionAsync(
        string name,
        string? description,
        string distributionName,
        bool includeGlobalSettings = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing template.
    /// </summary>
    /// <param name="template">The template with updated values.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The updated template.</returns>
    /// <exception cref="InvalidOperationException">Thrown if attempting to update a built-in template.</exception>
    Task<ConfigurationTemplate> UpdateTemplateAsync(ConfigurationTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a template.
    /// </summary>
    /// <param name="templateId">The ID of the template to delete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the template was deleted, false if not found.</returns>
    /// <exception cref="InvalidOperationException">Thrown if attempting to delete a built-in template.</exception>
    Task<bool> DeleteTemplateAsync(string templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Duplicates a template (including built-in templates).
    /// </summary>
    /// <param name="templateId">The ID of the template to duplicate.</param>
    /// <param name="newName">Optional new name for the duplicate.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The duplicated template.</returns>
    Task<ConfigurationTemplate> DuplicateTemplateAsync(string templateId, string? newName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a template to a distribution.
    /// </summary>
    /// <param name="templateId">The ID of the template to apply.</param>
    /// <param name="distributionName">The distribution to apply the template to.</param>
    /// <param name="options">Options for how to apply the template.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of the apply operation.</returns>
    Task<TemplateApplyResult> ApplyTemplateAsync(
        string templateId,
        string distributionName,
        TemplateApplyOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a template to multiple distributions.
    /// </summary>
    /// <param name="templateId">The ID of the template to apply.</param>
    /// <param name="distributionNames">The distributions to apply the template to.</param>
    /// <param name="options">Options for how to apply the template.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A dictionary of distribution names to apply results.</returns>
    Task<IReadOnlyDictionary<string, TemplateApplyResult>> ApplyTemplateToMultipleAsync(
        string templateId,
        IEnumerable<string> distributionNames,
        TemplateApplyOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a template to a JSON file.
    /// </summary>
    /// <param name="templateId">The ID of the template to export.</param>
    /// <param name="filePath">The path to export to.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task ExportTemplateAsync(string templateId, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a template from a JSON file.
    /// </summary>
    /// <param name="filePath">The path to import from.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The imported template.</returns>
    Task<ConfigurationTemplate> ImportTemplateAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Previews the changes that would be made by applying a template.
    /// </summary>
    /// <param name="templateId">The ID of the template.</param>
    /// <param name="distributionName">The distribution to preview changes for.</param>
    /// <param name="options">Options for how the template would be applied.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A description of the changes that would be made.</returns>
    Task<TemplatePreviewResult> PreviewTemplateAsync(
        string templateId,
        string distributionName,
        TemplateApplyOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of previewing a template application.
/// </summary>
public sealed record TemplatePreviewResult
{
    /// <summary>
    /// Gets the changes that would be made to global settings.
    /// </summary>
    public IReadOnlyList<SettingChange> GlobalChanges { get; init; } = [];

    /// <summary>
    /// Gets the changes that would be made to distribution settings.
    /// </summary>
    public IReadOnlyList<SettingChange> DistroChanges { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether any changes would be made.
    /// </summary>
    public bool HasChanges => GlobalChanges.Count > 0 || DistroChanges.Count > 0;
}

/// <summary>
/// Represents a single setting change.
/// </summary>
public sealed record SettingChange
{
    /// <summary>
    /// Gets the section name (e.g., "wsl2", "automount").
    /// </summary>
    public string Section { get; init; } = string.Empty;

    /// <summary>
    /// Gets the setting name.
    /// </summary>
    public string Setting { get; init; } = string.Empty;

    /// <summary>
    /// Gets the current value (null if not set).
    /// </summary>
    public string? CurrentValue { get; init; }

    /// <summary>
    /// Gets the new value from the template (null if not set in template).
    /// </summary>
    public string? NewValue { get; init; }

    /// <summary>
    /// Gets the type of change.
    /// </summary>
    public SettingChangeType ChangeType { get; init; }
}

/// <summary>
/// Type of setting change.
/// </summary>
public enum SettingChangeType
{
    /// <summary>
    /// A new setting will be added.
    /// </summary>
    Add,

    /// <summary>
    /// An existing setting will be modified.
    /// </summary>
    Modify,

    /// <summary>
    /// An existing setting will be removed.
    /// </summary>
    Remove,

    /// <summary>
    /// The setting value is unchanged.
    /// </summary>
    Unchanged
}
