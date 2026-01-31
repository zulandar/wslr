namespace Wslr.UI.Services;

/// <summary>
/// Service for managing the system tray icon.
/// </summary>
public interface ITrayIconService
{
    /// <summary>
    /// Initializes the tray icon.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Shows the tray icon.
    /// </summary>
    void Show();

    /// <summary>
    /// Hides the tray icon.
    /// </summary>
    void Hide();

    /// <summary>
    /// Updates the tray icon tooltip.
    /// </summary>
    /// <param name="tooltip">The tooltip text.</param>
    void UpdateTooltip(string tooltip);

    /// <summary>
    /// Shows a balloon notification.
    /// </summary>
    /// <param name="title">The notification title.</param>
    /// <param name="message">The notification message.</param>
    /// <param name="icon">The notification icon type.</param>
    void ShowBalloonTip(string title, string message, NotificationIcon icon = NotificationIcon.Info);

    /// <summary>
    /// Disposes of the tray icon resources.
    /// </summary>
    void Dispose();
}

/// <summary>
/// Notification icon types for balloon tips.
/// </summary>
public enum NotificationIcon
{
    /// <summary>
    /// No icon.
    /// </summary>
    None,

    /// <summary>
    /// Information icon.
    /// </summary>
    Info,

    /// <summary>
    /// Warning icon.
    /// </summary>
    Warning,

    /// <summary>
    /// Error icon.
    /// </summary>
    Error
}
