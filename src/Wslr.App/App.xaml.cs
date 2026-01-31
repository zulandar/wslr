using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wslr.App.Services;
using Wslr.Infrastructure;
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
