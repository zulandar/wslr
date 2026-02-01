using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
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
    /// Gets or sets the logging level switch for dynamic log level changes.
    /// </summary>
    public static LoggingLevelSwitch LoggingLevelSwitch { get; } = new(LogEventLevel.Information);

    /// <summary>
    /// Gets the path to the logs directory.
    /// </summary>
    public static string LogsPath { get; } = GetLogsPath();

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
        ConfigureLogging();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services);
            })
            .Build();
    }

    private static string GetLogsPath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataPath, "WSLR", "Logs");
    }

    private static void ConfigureLogging()
    {
        Directory.CreateDirectory(LogsPath);

        var logFilePath = Path.Combine(LogsPath, "wslr-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(LoggingLevelSwitch)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                fileSizeLimitBytes: 7 * 1024 * 1024, // 7MB per file, ~50MB total max
                rollOnFileSizeLimit: true,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    /// <summary>
    /// Sets the debug logging mode.
    /// </summary>
    /// <param name="enabled">Whether debug logging should be enabled.</param>
    public static void SetDebugLogging(bool enabled)
    {
        LoggingLevelSwitch.MinimumLevel = enabled ? LogEventLevel.Debug : LogEventLevel.Information;
        Log.Information("Debug logging {Status}", enabled ? "enabled" : "disabled");
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Infrastructure layer
        services.AddInfrastructure();

        // UI layer
        services.AddUI();

        // App layer services
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ILoggingService, LoggingService>();
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
        // Set up global exception handling
        DispatcherUnhandledException += (sender, args) =>
        {
            Log.Error(args.Exception, "Unhandled exception in dispatcher");
            MessageBox.Show(
                $"An error occurred:\n\n{args.Exception.Message}\n\nSee logs for details.",
                "WSLR Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                Log.Fatal(ex, "Fatal unhandled exception");
            }
        };

        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            Log.Error(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };

        // Log startup
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        var versionString = version is not null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "unknown";
        Log.Information("WSLR {Version} starting on {OS}", versionString, Environment.OSVersion);

        // Show splash screen on a separate thread for smooth animations
        using var splash = new SplashScreenManager();
        splash.Show();

        var startTime = DateTime.UtcNow;
        const int minimumSplashTimeMs = 1500; // Minimum time to show splash

        try
        {
            // Initialize host
            splash.UpdateStatus("Initializing services...");
            await _host.StartAsync();

            // Initialize debug logging from settings
            var settingsService = Services.GetRequiredService<ISettingsService>();
            var debugEnabled = settingsService.Get(SettingKeys.DebugLoggingEnabled, false);
            SetDebugLogging(debugEnabled);

            // Initialize tray icon
            splash.UpdateStatus("Setting up system tray...");
            var trayService = Services.GetRequiredService<ITrayIconService>();
            trayService.Initialize();
            trayService.Show();

            // Start distribution monitoring
            splash.UpdateStatus("Connecting to WSL...");
            var monitorService = Services.GetRequiredService<IDistributionMonitorService>();
            monitorService.StartMonitoring();

            // Wait for initial refresh to complete
            splash.UpdateStatus("Loading distributions...");
            await monitorService.RefreshAsync();

            // Check if we should start minimized to tray
            var startMinimized = settingsService.Get(SettingKeys.StartMinimized, false);

            // Prepare main window (heavy XAML initialization)
            splash.UpdateStatus("Preparing interface...");
            var mainWindow = Services.GetRequiredService<MainWindow>();

            // Load initial data
            splash.UpdateStatus("Ready");
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
            await splash.CloseAsync();

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
            Log.Fatal(ex, "Fatal error during startup");
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
        Log.Information("WSLR shutting down");

        // Stop monitoring
        var monitorService = Services.GetRequiredService<IDistributionMonitorService>();
        monitorService.Dispose();

        // Dispose tray icon
        var trayService = Services.GetRequiredService<ITrayIconService>();
        trayService.Dispose();

        await _host.StopAsync();
        _host.Dispose();

        // Flush and close Serilog
        await Log.CloseAndFlushAsync();

        base.OnExit(e);
    }
}
