using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.EventSourcing.Brooks.Abstractions.Storage;

/// <summary>
///     Provides extension methods for registering brook storage providers in the dependency injection container.
///     Simplifies the configuration of storage providers with various configuration options.
/// </summary>
public static class BrookStorageProviderHelpers
{
    /// <summary>
    ///     Registers a brook storage provider in the service collection without additional configuration.
    /// </summary>
    /// <typeparam name="TProvider">The type of the storage provider that implements <see cref="IBrookStorageProvider" />.</typeparam>
    /// <param name="services">The service collection to register the provider in.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection RegisterBrookStorageProvider<TProvider>(
        this IServiceCollection services
    )
        where TProvider : class, IBrookStorageProvider
    {
        services.AddSingleton<IBrookStorageWriter, TProvider>();
        services.AddSingleton<IBrookStorageReader, TProvider>();
        return services;
    }

    /// <summary>
    ///     Registers a brook storage provider in the service collection with configuration options.
    /// </summary>
    /// <typeparam name="TProvider">The type of the storage provider that implements <see cref="IBrookStorageProvider" />.</typeparam>
    /// <typeparam name="TOptions">The type of the configuration options.</typeparam>
    /// <param name="services">The service collection to register the provider in.</param>
    /// <param name="configureOptions">An action to configure the options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection RegisterBrookStorageProvider<TProvider, TOptions>(
        this IServiceCollection services,
        Action<TOptions> configureOptions
    )
        where TProvider : class, IBrookStorageProvider
        where TOptions : class, new()
    {
        services.Configure(configureOptions);
        return services.RegisterBrookStorageProvider<TProvider>();
    }

    /// <summary>
    ///     Registers a brook storage provider in the service collection with configuration from a configuration section.
    /// </summary>
    /// <typeparam name="TProvider">The type of the storage provider that implements <see cref="IBrookStorageProvider" />.</typeparam>
    /// <typeparam name="TOptions">The type of the configuration options.</typeparam>
    /// <param name="services">The service collection to register the provider in.</param>
    /// <param name="configurationSection">The configuration section containing the options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection RegisterBrookStorageProvider<TProvider, TOptions>(
        this IServiceCollection services,
        IConfiguration configurationSection
    )
        where TProvider : class, IBrookStorageProvider
        where TOptions : class, new()
    {
        services.Configure<TOptions>(configurationSection);
        return services.RegisterBrookStorageProvider<TProvider>();
    }
}