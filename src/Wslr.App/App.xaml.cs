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

        // Windows
        services.AddSingleton<MainWindow>();
    }

    /// <inheritdoc />
    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        // Initialize tray icon
        var trayService = Services.GetRequiredService<ITrayIconService>();
        trayService.Initialize();
        trayService.Show();

        // Start distribution monitoring (enables auto-refresh for tray and main window)
        var monitorService = Services.GetRequiredService<IDistributionMonitorService>();
        monitorService.StartMonitoring();

        // Show main window
        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        // Load initial data (monitor service will have already done first refresh)
        var mainViewModel = Services.GetRequiredService<MainWindowViewModel>();
        if (mainViewModel.CurrentViewModel is DistributionListViewModel distributionListViewModel)
        {
            // Sync the UI checkbox state with monitoring
            distributionListViewModel.IsAutoRefreshEnabled = true;
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
