namespace Mississippi.AspNetCore.Orleans.OutputCaching;

using System;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mississippi.AspNetCore.Orleans.OutputCaching.Options;

/// <summary>
/// Provides service registration for Orleans-backed output caching.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    /// Registers Orleans-backed output caching with default configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddOrleansOutputCaching(this IServiceCollection services)
    {
        return services.AddOrleansOutputCaching(_ => { });
    }

    /// <summary>
    /// Registers Orleans-backed output caching with configuration action.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure the output cache options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddOrleansOutputCaching(
        this IServiceCollection services,
        Action<OrleansOutputCacheOptions> configureOptions)
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
        return AddOrleansOutputCachingCore(services);
    }

    /// <summary>
    /// Registers Orleans-backed output caching with IConfiguration binding.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration section.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddOrleansOutputCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        services.Configure<OrleansOutputCacheOptions>(configuration);
        return AddOrleansOutputCachingCore(services);
    }

    private static IServiceCollection AddOrleansOutputCachingCore(IServiceCollection services)
    {
        services.AddSingleton<IOutputCacheStore, OrleansOutputCacheStore>();
        return services;
    }
}
