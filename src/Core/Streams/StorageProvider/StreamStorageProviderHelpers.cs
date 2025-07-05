using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Core.Streams.StorageProvider;

public static class StreamStorageProviderHelpers
{
// current helper – stays exactly as-is
    public static IServiceCollection RegisterStreamStorageProvider<TProvider>(
        this IServiceCollection services
    )
        where TProvider : class, IStreamStorageProvider
    {
        services.AddSingleton<IStreamStorageWriter, TProvider>();
        services.AddSingleton<IStreamStorageReader, TProvider>();
        return services;
    }

    // new helper – lets callers configure options for the provider
    public static IServiceCollection RegisterStreamStorageProvider<TProvider, TOptions>(
        this IServiceCollection services,
        Action<TOptions> configureOptions
    )
        where TProvider : class, IStreamStorageProvider // provider implementation
        where TOptions : class, new() // its options record
    {
        // Make the options available through IOptions<TOptions>/IOptionsMonitor<TOptions>
        services.Configure(configureOptions);

        // Reuse your original helper so the registration logic stays in one place
        return services.RegisterStreamStorageProvider<TProvider>();
    }

    // –— optional convenience if you prefer binding from IConfiguration ——
    public static IServiceCollection RegisterStreamStorageProvider<TProvider, TOptions>(
        this IServiceCollection services,
        IConfiguration configurationSection
    )
        where TProvider : class, IStreamStorageProvider
        where TOptions : class, new()
    {
        services.Configure<TOptions>(configurationSection);
        return services.RegisterStreamStorageProvider<TProvider>();
    }
}