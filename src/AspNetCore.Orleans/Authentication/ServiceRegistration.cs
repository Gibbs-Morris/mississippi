using System;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.AspNetCore.Orleans.Authentication.Options;


namespace Mississippi.AspNetCore.Orleans.Authentication;

/// <summary>
///     Provides service registration for Orleans-backed ticket store.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    ///     Registers Orleans-backed ticket store with default configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddOrleansTicketStore(
        this IServiceCollection services
    )
    {
        return services.AddOrleansTicketStore(_ => { });
    }

    /// <summary>
    ///     Registers Orleans-backed ticket store with configuration action.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure the ticket store options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddOrleansTicketStore(
        this IServiceCollection services,
        Action<TicketStoreOptions> configureOptions
    )
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
        return AddOrleansTicketStoreCore(services);
    }

    /// <summary>
    ///     Registers Orleans-backed ticket store with IConfiguration binding.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration section.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddOrleansTicketStore(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        services.Configure<TicketStoreOptions>(configuration);
        return AddOrleansTicketStoreCore(services);
    }

    private static IServiceCollection AddOrleansTicketStoreCore(
        IServiceCollection services
    )
    {
        services.AddSingleton<TicketSerializer>();
        services.AddSingleton<ITicketStore, OrleansTicketStore>();
        return services;
    }
}