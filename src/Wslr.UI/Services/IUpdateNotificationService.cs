namespace Wslr.UI.Services;

/// <summary>
/// Service for checking and displaying update notifications.
/// </summary>
public interface IUpdateNotificationService
{
    /// <summary>
    /// Checks for updates and shows a notification if one is available.
    /// This method is designed to be called on startup and handles all errors gracefully.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CheckAndNotifyAsync(CancellationToken cancellationToken = default);
}
