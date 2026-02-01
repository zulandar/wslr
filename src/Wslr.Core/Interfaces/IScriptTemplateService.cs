using Wslr.Core.Models;

namespace Wslr.Core.Interfaces;

/// <summary>
/// Service for managing setup script templates.
/// </summary>
public interface IScriptTemplateService
{
    /// <summary>
    /// Gets all available templates (built-in and user-created).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all templates, ordered by built-in status then name.</returns>
    Task<IReadOnlyList<ScriptTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets templates filtered by category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of templates in the specified category.</returns>
    Task<IReadOnlyList<ScriptTemplate>> GetTemplatesByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a template by its ID.
    /// </summary>
    /// <param name="templateId">The template ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The template if found, null otherwise.</returns>
    Task<ScriptTemplate?> GetTemplateAsync(string templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new template.
    /// </summary>
    /// <param name="template">The template to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created template with assigned ID.</returns>
    Task<ScriptTemplate> CreateTemplateAsync(ScriptTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing template.
    /// </summary>
    /// <param name="template">The template with updated values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated template.</returns>
    /// <exception cref="InvalidOperationException">If the template is built-in or doesn't exist.</exception>
    Task<ScriptTemplate> UpdateTemplateAsync(ScriptTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a template.
    /// </summary>
    /// <param name="templateId">The ID of the template to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    /// <exception cref="InvalidOperationException">If the template is built-in.</exception>
    Task<bool> DeleteTemplateAsync(string templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Duplicates a template (including built-in templates).
    /// </summary>
    /// <param name="templateId">The ID of the template to duplicate.</param>
    /// <param name="newName">Optional new name for the duplicate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new template copy.</returns>
    Task<ScriptTemplate> DuplicateTemplateAsync(string templateId, string? newName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a template to JSON format.
    /// </summary>
    /// <param name="templateId">The ID of the template to export.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The template as a JSON string.</returns>
    Task<string> ExportTemplateAsync(string templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a template from JSON format.
    /// </summary>
    /// <param name="json">The JSON string representing the template.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The imported template.</returns>
    Task<ScriptTemplate> ImportTemplateAsync(string json, CancellationToken cancellationToken = default);
}
