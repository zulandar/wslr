namespace Wslr.UI.Services;

/// <summary>
/// Service for displaying dialogs to the user.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows a confirmation dialog.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The dialog message.</param>
    /// <returns>True if the user confirmed, false otherwise.</returns>
    Task<bool> ShowConfirmationAsync(string title, string message);

    /// <summary>
    /// Shows an error dialog.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The error message.</param>
    Task ShowErrorAsync(string title, string message);

    /// <summary>
    /// Shows an information dialog.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The information message.</param>
    Task ShowInfoAsync(string title, string message);

    /// <summary>
    /// Shows a file save dialog.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="defaultFileName">The default file name.</param>
    /// <param name="filter">The file filter (e.g., "Tar files|*.tar").</param>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> ShowSaveFileDialogAsync(string title, string defaultFileName, string filter);

    /// <summary>
    /// Shows a file open dialog.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="filter">The file filter (e.g., "Tar files|*.tar").</param>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> ShowOpenFileDialogAsync(string title, string filter);

    /// <summary>
    /// Shows a folder browser dialog.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <returns>The selected folder path, or null if cancelled.</returns>
    Task<string?> ShowFolderBrowserDialogAsync(string title);
}
