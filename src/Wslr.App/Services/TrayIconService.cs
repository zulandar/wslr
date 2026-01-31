using System.Windows;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.Extensions.DependencyInjection;
using Wslr.UI.Services;
using Wslr.UI.ViewModels;

namespace Wslr.App.Services;

/// <summary>
/// Implementation of <see cref="ITrayIconService"/> using H.NotifyIcon.
/// </summary>
public class TrayIconService : ITrayIconService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private TaskbarIcon? _taskbarIcon;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrayIconService"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public TrayIconService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public void Initialize()
    {
        _taskbarIcon = new TaskbarIcon
        {
            ToolTipText = "WSLR - WSL Instance Manager",
            ContextMenu = CreateContextMenu()
        };

        _taskbarIcon.TrayMouseDoubleClick += (_, _) =>
        {
            var navigationService = _serviceProvider.GetRequiredService<INavigationService>();
            navigationService.ShowMainWindow();
        };
    }

    /// <inheritdoc />
    public void Show()
    {
        if (_taskbarIcon is not null)
        {
            _taskbarIcon.Visibility = Visibility.Visible;
        }
    }

    /// <inheritdoc />
    public void Hide()
    {
        if (_taskbarIcon is not null)
        {
            _taskbarIcon.Visibility = Visibility.Collapsed;
        }
    }

    /// <inheritdoc />
    public void UpdateTooltip(string tooltip)
    {
        if (_taskbarIcon is not null)
        {
            _taskbarIcon.ToolTipText = tooltip;
        }
    }

    /// <inheritdoc />
    public void ShowBalloonTip(string title, string message, NotificationIcon icon = NotificationIcon.Info)
    {
        _taskbarIcon?.ShowNotification(title, message, MapNotificationIcon(icon));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _taskbarIcon?.Dispose();
        _taskbarIcon = null;
        _disposed = true;
    }

    private System.Windows.Controls.ContextMenu CreateContextMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        var openItem = new System.Windows.Controls.MenuItem { Header = "Open WSLR" };
        openItem.Click += (_, _) =>
        {
            var navigationService = _serviceProvider.GetRequiredService<INavigationService>();
            navigationService.ShowMainWindow();
        };

        var refreshItem = new System.Windows.Controls.MenuItem { Header = "Refresh" };
        refreshItem.Click += async (_, _) =>
        {
            var trayViewModel = _serviceProvider.GetRequiredService<TrayIconViewModel>();
            await trayViewModel.RefreshDistributionsCommand.ExecuteAsync(null);
        };

        var shutdownItem = new System.Windows.Controls.MenuItem { Header = "Shutdown All WSL" };
        shutdownItem.Click += async (_, _) =>
        {
            var trayViewModel = _serviceProvider.GetRequiredService<TrayIconViewModel>();
            await trayViewModel.ShutdownAllCommand.ExecuteAsync(null);
        };

        var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) =>
        {
            var navigationService = _serviceProvider.GetRequiredService<INavigationService>();
            navigationService.ExitApplication();
        };

        menu.Items.Add(openItem);
        menu.Items.Add(new System.Windows.Controls.Separator());
        menu.Items.Add(refreshItem);
        menu.Items.Add(shutdownItem);
        menu.Items.Add(new System.Windows.Controls.Separator());
        menu.Items.Add(exitItem);

        return menu;
    }

    private static NotificationIconType MapNotificationIcon(NotificationIcon icon)
    {
        return icon switch
        {
            NotificationIcon.Info => NotificationIconType.Info,
            NotificationIcon.Warning => NotificationIconType.Warning,
            NotificationIcon.Error => NotificationIconType.Error,
            _ => NotificationIconType.None
        };
    }
}
