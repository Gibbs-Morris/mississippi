using System;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.AspNetCore.Orleans.Caching.Options;


namespace Mississippi.AspNetCore.Orleans.Caching;

/// <summary>
///     Provides service registration for Orleans-backed distributed cache.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    ///     Registers Orleans-backed distributed cache with default configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddOrleansDistributedCache(
        this IServiceCollection services
    )
    {
        return services.AddOrleansDistributedCache(_ => { });
    }

    /// <summary>
    ///     Registers Orleans-backed distributed cache with configuration action.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure the distributed cache options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddOrleansDistributedCache(
        this IServiceCollection services,
        Action<DistributedCacheOptions> configureOptions
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
        return AddOrleansDistributedCacheCore(services);
    }

    /// <summary>
    ///     Registers Orleans-backed distributed cache with IConfiguration binding.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration section containing DistributedCacheOptions.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddOrleansDistributedCache(
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

        services.Configure<DistributedCacheOptions>(configuration);
        return AddOrleansDistributedCacheCore(services);
    }

    private static IServiceCollection AddOrleansDistributedCacheCore(
        IServiceCollection services
    )
    {
        services.AddSingleton<IDistributedCache, OrleansDistributedCache>();
        return services;
    }
}