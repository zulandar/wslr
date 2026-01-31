using Wslr.Core.Interfaces;
using Wslr.UI.Services;

namespace Wslr.App.Services;

/// <summary>
/// Service that checks for updates and displays notifications via the system tray.
/// </summary>
public class UpdateNotificationService : IUpdateNotificationService
{
    private readonly IUpdateChecker _updateChecker;
    private readonly ITrayIconService _trayIconService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateNotificationService"/> class.
    /// </summary>
    /// <param name="updateChecker">The update checker service.</param>
    /// <param name="trayIconService">The tray icon service.</param>
    public UpdateNotificationService(IUpdateChecker updateChecker, ITrayIconService trayIconService)
    {
        _updateChecker = updateChecker ?? throw new ArgumentNullException(nameof(updateChecker));
        _trayIconService = trayIconService ?? throw new ArgumentNullException(nameof(trayIconService));
    }

    /// <inheritdoc />
    public async Task CheckAndNotifyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _updateChecker.CheckForUpdatesAsync(cancellationToken);

            if (!result.UpdateAvailable || result.LatestVersion is null)
            {
                return;
            }

            var title = "Update Available";
            var message = $"WSLR {result.LatestVersion} is available. Click to download.";

            if (!string.IsNullOrEmpty(result.ReleaseUrl))
            {
                _trayIconService.ShowBalloonTipWithUrl(title, message, result.ReleaseUrl, NotificationIcon.Info);
            }
            else
            {
                _trayIconService.ShowBalloonTip(title, message, NotificationIcon.Info);
            }
        }
        catch
        {
            // Silently ignore any errors - update check should never crash the app
        }
    }
}
