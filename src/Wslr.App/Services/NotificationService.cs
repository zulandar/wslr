using Wslr.UI.Services;

namespace Wslr.App.Services;

/// <summary>
/// Implementation of <see cref="INotificationService"/> using the tray icon.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ITrayIconService _trayIconService;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationService"/> class.
    /// </summary>
    /// <param name="trayIconService">The tray icon service.</param>
    public NotificationService(ITrayIconService trayIconService)
    {
        _trayIconService = trayIconService ?? throw new ArgumentNullException(nameof(trayIconService));
    }

    /// <inheritdoc />
    public void ShowInfo(string title, string message)
    {
        _trayIconService.ShowBalloonTip(title, message, NotificationIcon.Info);
    }

    /// <inheritdoc />
    public void ShowSuccess(string title, string message)
    {
        _trayIconService.ShowBalloonTip(title, message, NotificationIcon.Info);
    }

    /// <inheritdoc />
    public void ShowWarning(string title, string message)
    {
        _trayIconService.ShowBalloonTip(title, message, NotificationIcon.Warning);
    }

    /// <inheritdoc />
    public void ShowError(string title, string message)
    {
        _trayIconService.ShowBalloonTip(title, message, NotificationIcon.Error);
    }
}
