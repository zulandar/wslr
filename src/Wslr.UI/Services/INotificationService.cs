namespace Wslr.UI.Services;

/// <summary>
/// Service for displaying notifications to the user.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Shows an information notification.
    /// </summary>
    /// <param name="title">The notification title.</param>
    /// <param name="message">The notification message.</param>
    void ShowInfo(string title, string message);

    /// <summary>
    /// Shows a success notification.
    /// </summary>
    /// <param name="title">The notification title.</param>
    /// <param name="message">The notification message.</param>
    void ShowSuccess(string title, string message);

    /// <summary>
    /// Shows a warning notification.
    /// </summary>
    /// <param name="title">The notification title.</param>
    /// <param name="message">The notification message.</param>
    void ShowWarning(string title, string message);

    /// <summary>
    /// Shows an error notification.
    /// </summary>
    /// <param name="title">The notification title.</param>
    /// <param name="message">The notification message.</param>
    void ShowError(string title, string message);
}
