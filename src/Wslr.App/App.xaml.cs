using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wslr.App.Services;
using Wslr.Core.Interfaces;
using Wslr.Infrastructure;
using Wslr.Infrastructure.Services;
using Wslr.UI;
using Wslr.UI.Services;
using Wslr.UI.ViewModels;

namespace Wslr.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    /// <summary>
    /// Gets the current application instance.
    /// </summary>
    public new static App Current => (App)Application.Current;

    /// <summary>
    /// Gets the service provider.
    /// </summary>
    public IServiceProvider Services => _host.Services;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services);
            })
            .Build();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Infrastructure layer
        services.AddInfrastructure();

        // UI layer
        services.AddUI();

        // App layer services
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<ITrayIconService, TrayIconService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IStartupService, StartupService>();

        // Update checker services
        services.AddHttpClient("GitHubApi", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Wslr");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddSingleton<IUpdateChecker>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("GitHubApi");
            return new GitHubUpdateChecker(httpClient, "zulandar", "wslr");
        });
        services.AddSingleton<IUpdateNotificationService, UpdateNotificationService>();

        // Windows
        services.AddSingleton<MainWindow>();
    }

    /// <inheritdoc />
    protected override async void OnStartup(StartupEventArgs e)
    {
        // Show splash screen immediately
        var splash = new SplashScreen();
        splash.Show();

        var startTime = DateTime.UtcNow;
        const int minimumSplashTimeMs = 1500; // Minimum time to show splash

        try
        {
            // Initialize host
            splash.UpdateStatus("Initializing services...");
            await _host.StartAsync();

            // Initialize tray icon
            splash.UpdateStatus("Setting up system tray...");
            var trayService = Services.GetRequiredService<ITrayIconService>();
            trayService.Initialize();
            trayService.Show();

            // Small delay to let UI updates show
            await Task.Delay(100);

            // Start distribution monitoring
            splash.UpdateStatus("Connecting to WSL...");
            var monitorService = Services.GetRequiredService<IDistributionMonitorService>();
            monitorService.StartMonitoring();

            // Wait for initial refresh to complete
            splash.UpdateStatus("Loading distributions...");
            await monitorService.RefreshAsync();

            // Check if we should start minimized to tray
            var settingsService = Services.GetRequiredService<ISettingsService>();
            var startMinimized = settingsService.Get(SettingKeys.StartMinimized, false);

            // Prepare main window
            splash.UpdateStatus("Ready");
            var mainWindow = Services.GetRequiredService<MainWindow>();

            // Load initial data
            var mainViewModel = Services.GetRequiredService<MainWindowViewModel>();
            if (mainViewModel.CurrentViewModel is DistributionListViewModel distributionListViewModel)
            {
                distributionListViewModel.IsAutoRefreshEnabled = true;
            }

            // Ensure minimum splash time for smooth UX
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            if (elapsed < minimumSplashTimeMs)
            {
                await Task.Delay((int)(minimumSplashTimeMs - elapsed));
            }

            // Fade out splash
            await splash.FadeOutAndCloseAsync();

            // Show main window only if not starting minimized
            if (!startMinimized)
            {
                mainWindow.Show();
            }

            // Check for updates in the background (non-blocking)
            _ = CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            splash.UpdateStatus($"Error: {ex.Message}");
            await Task.Delay(3000); // Show error for 3 seconds
            splash.Close();
            Shutdown(1);
            return;
        }

        base.OnStartup(e);
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            // Delay to let the app fully initialize and not compete with startup
            await Task.Delay(TimeSpan.FromSeconds(5));

            var updateService = Services.GetRequiredService<IUpdateNotificationService>();
            await updateService.CheckAndNotifyAsync();
        }
        catch
        {
            // Silently ignore any errors - update check should never crash the app
        }
    }

    /// <inheritdoc />
    protected override async void OnExit(ExitEventArgs e)
    {
        // Stop monitoring
        var monitorService = Services.GetRequiredService<IDistributionMonitorService>();
        monitorService.Dispose();

        // Dispose tray icon
        var trayService = Services.GetRequiredService<ITrayIconService>();
        trayService.Dispose();

        await _host.StopAsync();
        _host.Dispose();

        base.OnExit(e);
    }
}
