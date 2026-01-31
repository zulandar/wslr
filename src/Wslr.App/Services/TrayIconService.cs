using System.Windows;
using System.Windows.Controls;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.Extensions.DependencyInjection;
using Wslr.App.Helpers;
using Wslr.Core.Models;
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
    private MenuItem? _distributionsMenuItem;
    private bool _disposed;
    private TrayIconStatus _currentStatus = TrayIconStatus.Default;
    private string? _pendingNotificationUrl;
    private bool _notificationClickSubscribed;

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
        var icon = IconHelper.CreateTrayIcon();

        _taskbarIcon = new TaskbarIcon
        {
            Icon = icon,
            ToolTipText = "WSLR - WSL Instance Manager",
            ContextMenu = CreateContextMenu()
        };

        // Force create the tray icon immediately
        _taskbarIcon.ForceCreate();

        _taskbarIcon.TrayMouseDoubleClick += (_, _) =>
        {
            var navigationService = _serviceProvider.GetRequiredService<INavigationService>();
            navigationService.ShowMainWindow();
        };

        // Subscribe to distribution changes
        var monitorService = _serviceProvider.GetRequiredService<IDistributionMonitorService>();
        monitorService.DistributionsRefreshed += OnDistributionsRefreshed;
    }

    private void OnDistributionsRefreshed(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            RefreshContextMenu();
            UpdateStatusFromDistributions();
            UpdateTooltipFromDistributions();
        });
    }

    private void UpdateStatusFromDistributions()
    {
        var monitorService = _serviceProvider.GetRequiredService<IDistributionMonitorService>();
        var distributions = monitorService.Distributions;
        var hasRunning = distributions.Any(d => d.State == DistributionState.Running);
        UpdateStatus(hasRunning);
    }

    private void UpdateTooltipFromDistributions()
    {
        var monitorService = _serviceProvider.GetRequiredService<IDistributionMonitorService>();
        var distributions = monitorService.Distributions;
        var total = distributions.Count;
        var running = distributions.Count(d => d.State == DistributionState.Running);

        string tooltip;
        if (total == 0)
        {
            tooltip = "WSLR - No distributions installed";
        }
        else if (running == 0)
        {
            tooltip = "WSLR - All distributions stopped";
        }
        else if (running == 1)
        {
            var runningName = distributions.First(d => d.State == DistributionState.Running).Name;
            tooltip = $"WSLR - {runningName} running";
        }
        else
        {
            tooltip = $"WSLR - {running} of {total} distributions running";
        }

        UpdateTooltip(tooltip);
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
    public void UpdateStatus(bool hasRunningDistributions)
    {
        if (_taskbarIcon is null)
        {
            return;
        }

        var newStatus = hasRunningDistributions ? TrayIconStatus.Running : TrayIconStatus.Stopped;
        if (newStatus == _currentStatus)
        {
            return;
        }

        _currentStatus = newStatus;
        var oldIcon = _taskbarIcon.Icon;
        _taskbarIcon.Icon = IconHelper.CreateTrayIcon(newStatus);
        oldIcon?.Dispose();
    }

    /// <inheritdoc />
    public void RefreshContextMenu()
    {
        if (_distributionsMenuItem is null)
        {
            return;
        }

        _distributionsMenuItem.Items.Clear();

        var monitorService = _serviceProvider.GetRequiredService<IDistributionMonitorService>();
        var distributions = monitorService.Distributions.OrderBy(d => d.Name).ToList();

        if (distributions.Count == 0)
        {
            var emptyItem = new MenuItem
            {
                Header = "(No distributions)",
                IsEnabled = false
            };
            _distributionsMenuItem.Items.Add(emptyItem);
            return;
        }

        foreach (var distribution in distributions)
        {
            var distroItem = new MenuItem
            {
                Header = distribution.Name
            };

            var isRunning = distribution.State == DistributionState.Running;

            // Status indicator
            var statusItem = new MenuItem
            {
                Header = isRunning ? "● Running" : "○ Stopped",
                IsEnabled = false
            };
            distroItem.Items.Add(statusItem);
            distroItem.Items.Add(new Separator());

            if (isRunning)
            {
                var stopItem = new MenuItem { Header = "Stop" };
                var distroName = distribution.Name;
                stopItem.Click += async (_, _) =>
                {
                    var trayViewModel = _serviceProvider.GetRequiredService<TrayIconViewModel>();
                    await trayViewModel.StopDistributionCommand.ExecuteAsync(distroName);
                };
                distroItem.Items.Add(stopItem);
            }
            else
            {
                var startItem = new MenuItem { Header = "Start" };
                var distroName = distribution.Name;
                startItem.Click += async (_, _) =>
                {
                    var trayViewModel = _serviceProvider.GetRequiredService<TrayIconViewModel>();
                    await trayViewModel.StartDistributionCommand.ExecuteAsync(distroName);
                };
                distroItem.Items.Add(startItem);
            }

            _distributionsMenuItem.Items.Add(distroItem);
        }
    }

    /// <inheritdoc />
    public void ShowBalloonTip(string title, string message, UI.Services.NotificationIcon icon = UI.Services.NotificationIcon.Info)
    {
        _taskbarIcon?.ShowNotification(title, message, MapNotificationIcon(icon));
    }

    /// <inheritdoc />
    public void ShowBalloonTipWithUrl(string title, string message, string url, UI.Services.NotificationIcon icon = UI.Services.NotificationIcon.Info)
    {
        if (_taskbarIcon is null)
        {
            return;
        }

        // Store the URL for the click handler
        _pendingNotificationUrl = url;

        // Subscribe to the click event (if not already)
        if (!_notificationClickSubscribed)
        {
            _taskbarIcon.TrayBalloonTipClicked += OnBalloonTipClicked;
            _notificationClickSubscribed = true;
        }

        _taskbarIcon.ShowNotification(title, message, MapNotificationIcon(icon));
    }

    private void OnBalloonTipClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_pendingNotificationUrl))
        {
            return;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = _pendingNotificationUrl,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore errors opening URL
        }

        _pendingNotificationUrl = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Unsubscribe from events
        var monitorService = _serviceProvider.GetService<IDistributionMonitorService>();
        if (monitorService is not null)
        {
            monitorService.DistributionsRefreshed -= OnDistributionsRefreshed;
        }

        _taskbarIcon?.Dispose();
        _taskbarIcon = null;
        _disposed = true;
    }

    private ContextMenu CreateContextMenu()
    {
        var menu = new ContextMenu();

        var openItem = new MenuItem { Header = "Open WSLR" };
        openItem.Click += (_, _) =>
        {
            var navigationService = _serviceProvider.GetRequiredService<INavigationService>();
            navigationService.ShowMainWindow();
        };

        // Distributions submenu
        _distributionsMenuItem = new MenuItem { Header = "Distributions" };
        var loadingItem = new MenuItem
        {
            Header = "Loading...",
            IsEnabled = false
        };
        _distributionsMenuItem.Items.Add(loadingItem);

        var refreshItem = new MenuItem { Header = "Refresh" };
        refreshItem.Click += async (_, _) =>
        {
            var trayViewModel = _serviceProvider.GetRequiredService<TrayIconViewModel>();
            await trayViewModel.RefreshDistributionsCommand.ExecuteAsync(null);
        };

        var shutdownItem = new MenuItem { Header = "Shutdown All WSL" };
        shutdownItem.Click += async (_, _) =>
        {
            var trayViewModel = _serviceProvider.GetRequiredService<TrayIconViewModel>();
            await trayViewModel.ShutdownAllCommand.ExecuteAsync(null);
        };

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) =>
        {
            var navigationService = _serviceProvider.GetRequiredService<INavigationService>();
            navigationService.ExitApplication();
        };

        menu.Items.Add(openItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(_distributionsMenuItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(refreshItem);
        menu.Items.Add(shutdownItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(exitItem);

        return menu;
    }

    private static H.NotifyIcon.Core.NotificationIcon MapNotificationIcon(UI.Services.NotificationIcon icon)
    {
        return icon switch
        {
            UI.Services.NotificationIcon.Info => H.NotifyIcon.Core.NotificationIcon.Info,
            UI.Services.NotificationIcon.Warning => H.NotifyIcon.Core.NotificationIcon.Warning,
            UI.Services.NotificationIcon.Error => H.NotifyIcon.Core.NotificationIcon.Error,
            _ => H.NotifyIcon.Core.NotificationIcon.None
        };
    }
}
