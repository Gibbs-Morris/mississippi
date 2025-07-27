using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Core.Abstractions.Providers.Storage;

public static class BrookStorageProviderHelpers
{
    public static IServiceCollection RegisterBrookStorageProvider<TProvider>(
        this IServiceCollection services
    )
        where TProvider : class, IBrookStorageProvider
    {
        services.AddSingleton<IBrookStorageWriter, TProvider>();
        services.AddSingleton<IBrookStorageReader, TProvider>();
        return services;
    }

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