using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Core.Abstractions.Providers.Projection;

/// <summary>
///     Helper methods for registering projection storage providers with dependency injection.
/// </summary>
public static class ProjectionStorageProviderHelpers
{
    /// <summary>
    ///     Registers a projection storage provider with the service collection.
    /// </summary>
    /// <typeparam name="TProvider">The type of projection storage provider to register.</typeparam>
    /// <param name="services">The service collection to register the provider with.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection RegisterProjectionStorageProvider<TProvider>(
        this IServiceCollection services
    )
        where TProvider : class, IProjectionStorageProvider
    {
        services.AddSingleton<IProjectionStorageReader, TProvider>();
        services.AddSingleton<IProjectionStorageWriter, TProvider>();
        return services;
    }

    /// <summary>
    ///     Registers a projection storage provider with options configuration via delegate.
    /// </summary>
    /// <typeparam name="TProvider">The type of projection storage provider to register.</typeparam>
    /// <typeparam name="TOptions">The type of options to configure.</typeparam>
    /// <param name="services">The service collection to register the provider with.</param>
    /// <param name="configureOptions">A delegate to configure the options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection RegisterProjectionStorageProvider<TProvider, TOptions>(
        this IServiceCollection services,
        Action<TOptions> configureOptions
    )
        where TProvider : class, IProjectionStorageProvider
        where TOptions : class, new()
    {
        services.Configure(configureOptions);
        return services.RegisterProjectionStorageProvider<TProvider>();
    }

    /// <summary>
    ///     Registers a projection storage provider with options bound from configuration.
    /// </summary>
    /// <typeparam name="TProvider">The type of projection storage provider to register.</typeparam>
    /// <typeparam name="TOptions">The type of options to configure.</typeparam>
    /// <param name="services">The service collection to register the provider with.</param>
    /// <param name="configurationSection">The configuration section to bind options from.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection RegisterProjectionStorageProvider<TProvider, TOptions>(
        this IServiceCollection services,
        IConfiguration configurationSection
    )
        where TProvider : class, IProjectionStorageProvider
        where TOptions : class, new()
    {
        services.Configure<TOptions>(configurationSection);
        return services.RegisterProjectionStorageProvider<TProvider>();
    }
}