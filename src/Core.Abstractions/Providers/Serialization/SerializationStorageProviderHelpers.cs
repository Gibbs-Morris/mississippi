using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mississippi.Core.Abstractions.Providers.Serialization;

public static class SerializationStorageProviderHelpers
{
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

    // Allows callers to register configuration actions for provider-specific options
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

    // Convenience overload for binding options from an IConfiguration section
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