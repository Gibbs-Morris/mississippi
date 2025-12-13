namespace Mississippi.AspNetCore.Orleans.SignalR;

using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mississippi.AspNetCore.Orleans.SignalR.Options;

/// <summary>
/// Provides service registration for Orleans-backed SignalR scale-out.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    /// Registers Orleans-backed SignalR scale-out with default configuration.
    /// </summary>
    /// <typeparam name="THub">The hub type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddOrleansSignalRScaleOut<THub>(this IServiceCollection services)
        where THub : Hub
    {
        return services.AddOrleansSignalRScaleOut<THub>(_ => { });
    }

    /// <summary>
    /// Registers Orleans-backed SignalR scale-out with configuration action.
    /// </summary>
    /// <typeparam name="THub">The hub type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure the SignalR options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddOrleansSignalRScaleOut<THub>(
        this IServiceCollection services,
        Action<SignalROptions> configureOptions)
        where THub : Hub
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configureOptions is null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        services.Configure(configureOptions);
        return AddOrleansSignalRScaleOutCore<THub>(services);
    }

    /// <summary>
    /// Registers Orleans-backed SignalR scale-out with IConfiguration binding.
    /// </summary>
    /// <typeparam name="THub">The hub type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration section.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddOrleansSignalRScaleOut<THub>(
        this IServiceCollection services,
        IConfiguration configuration)
        where THub : Hub
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        services.Configure<SignalROptions>(configuration);
        return AddOrleansSignalRScaleOutCore<THub>(services);
    }

    private static IServiceCollection AddOrleansSignalRScaleOutCore<THub>(IServiceCollection services)
        where THub : Hub
    {
        services.AddSingleton<HubLifetimeManager<THub>, OrleansHubLifetimeManager<THub>>();
        return services;
    }
}
