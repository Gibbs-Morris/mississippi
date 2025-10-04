using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Core.Abstractions.Providers.Serialization;

/// <summary>
///     Provides extension methods for registering serialization storage providers in dependency injection containers.
/// </summary>
public static class SerializationStorageProviderHelpers
{
    /// <summary>
    ///     Registers a serialization storage provider in the service collection.
    /// </summary>
    /// <typeparam name="TProvider">The type of the serialization provider to register.</typeparam>
    /// <param name="services">The service collection to register the provider in.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection RegisterSerializationStorageProvider<TProvider>(
        this IServiceCollection services
    )
        where TProvider : class, ISerializationProvider
    {
        services.AddSingleton<ISerializationReader, TProvider>();
        services.AddSingleton<IAsyncSerializationReader, TProvider>();
        services.AddSingleton<ISerializationWriter, TProvider>();
        services.AddSingleton<IAsyncSerializationWriter, TProvider>();
        return services;
    }

    /// <summary>
    ///     Registers a serialization storage provider with configuration options in the service collection.
    /// </summary>
    /// <typeparam name="TProvider">The type of the serialization provider to register.</typeparam>
    /// <typeparam name="TOptions">The type of the configuration options.</typeparam>
    /// <param name="services">The service collection to register the provider in.</param>
    /// <param name="configureOptions">An action to configure the provider options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection RegisterSerializationStorageProvider<TProvider, TOptions>(
        this IServiceCollection services,
        Action<TOptions> configureOptions
    )
        where TProvider : class, ISerializationProvider
        where TOptions : class, new()
    {
        services.Configure(configureOptions);
        return services.RegisterSerializationStorageProvider<TProvider>();
    }

    /// <summary>
    ///     Registers a serialization storage provider with configuration options from an IConfiguration section.
    /// </summary>
    /// <typeparam name="TProvider">The type of the serialization provider to register.</typeparam>
    /// <typeparam name="TOptions">The type of the configuration options.</typeparam>
    /// <param name="services">The service collection to register the provider in.</param>
    /// <param name="configurationSection">The configuration section to bind options from.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection RegisterSerializationStorageProvider<TProvider, TOptions>(
        this IServiceCollection services,
        IConfiguration configurationSection
    )
        where TProvider : class, ISerializationProvider
        where TOptions : class, new()
    {
        services.Configure<TOptions>(configurationSection);
        return services.RegisterSerializationStorageProvider<TProvider>();
    }
}
