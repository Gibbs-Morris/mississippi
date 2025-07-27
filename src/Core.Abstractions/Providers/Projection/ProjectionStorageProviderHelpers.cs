using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mississippi.Core.Abstractions.Providers.Projection;

public static class ProjectionStorageProviderHelpers
{
    public static IServiceCollection RegisterProjectionStorageProvider<TProvider>(
        this IServiceCollection services
    )
        where TProvider : class, IProjectionStorageProvider
    {
        services.AddSingleton<IProjectionStorageReader, TProvider>();
        services.AddSingleton<IProjectionStorageWriter, TProvider>();
        return services;
    }

    // Allows callers to configure provider-specific options via delegate
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

    // Overload that binds options from an IConfiguration section
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