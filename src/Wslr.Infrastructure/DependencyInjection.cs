using Microsoft.Extensions.DependencyInjection;
using Wslr.Core.Interfaces;
using Wslr.Infrastructure.Services;

namespace Wslr.Infrastructure;

/// <summary>
/// Extension methods for configuring infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<IWslService, WslService>();
        services.AddSingleton<IWslConfigService, WslConfigService>();
        services.AddSingleton<IWslDistroConfigService, WslDistroConfigService>();
        services.AddSingleton<ITerminalSessionService, TerminalSessionService>();

        return services;
    }
}
