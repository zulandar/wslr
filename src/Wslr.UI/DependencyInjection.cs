using Microsoft.Extensions.DependencyInjection;
using Wslr.UI.Services;
using Wslr.UI.ViewModels;

namespace Wslr.UI;

/// <summary>
/// Extension methods for configuring UI services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds UI services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddUI(this IServiceCollection services)
    {
        // Services
        services.AddSingleton<IDistributionMonitorService, DistributionMonitorService>();
        services.AddSingleton<IResourceMonitorService, ResourceMonitorService>();
        services.AddSingleton<IDistributionResourceService, DistributionResourceService>();

        // ViewModels
        services.AddTransient<DistributionListViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<TrayIconViewModel>();

        return services;
    }
}
