using System;

using Azure.Storage.Blobs;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using Mississippi.Tributary.Runtime.Storage.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blobs.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Blobs;

/// <summary>
///     Extension methods for registering Blob snapshot storage provider services.
/// </summary>
public static class SnapshotBlobStorageProviderRegistrations
{
    /// <summary>
    ///     Registers Blob snapshot storage provider services using an externally provided keyed
    ///     <see cref="BlobServiceClient" />.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddBlobSnapshotStorageProvider(
        this IServiceCollection services
    )
    {
        services.AddOptions<SnapshotBlobStorageOptions>().ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor
                .Singleton<IValidateOptions<SnapshotBlobStorageOptions>, SnapshotBlobStorageOptionsValidator>());
        services.AddSingleton<ISnapshotBlobOperations, SnapshotBlobOperations>();
        services.AddSingleton<ISnapshotBlobRepository, SnapshotBlobRepository>();
        services.RegisterSnapshotStorageProvider<SnapshotBlobStorageProvider>();
        services.AddHostedService<SnapshotBlobContainerInitializer>();

        // Caller must register a keyed BlobServiceClient with SnapshotBlobDefaults.BlobServiceClientServiceKey
        // or override SnapshotBlobStorageOptions.BlobServiceClientServiceKey to a different keyed client.
        services.AddKeyedSingleton<BlobContainerClient>(
            SnapshotBlobDefaults.BlobContainerClientServiceKey,
            (
                provider,
                _
            ) =>
            {
                SnapshotBlobStorageOptions options =
                    provider.GetRequiredService<IOptions<SnapshotBlobStorageOptions>>().Value;
                BlobServiceClient blobServiceClient =
                    provider.GetRequiredKeyedService<BlobServiceClient>(options.BlobServiceClientServiceKey);
                return blobServiceClient.GetBlobContainerClient(options.ContainerName);
            });
        return services;
    }

    /// <summary>
    ///     Creates and registers the keyed <see cref="BlobServiceClient" /> from the supplied connection string.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="blobConnectionString">The Azure Blob Storage connection string.</param>
    /// <param name="configureOptions">Optional options configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddBlobSnapshotStorageProvider(
        this IServiceCollection services,
        string blobConnectionString,
        Action<SnapshotBlobStorageOptions>? configureOptions = null
    )
    {
        services.AddKeyedSingleton<BlobServiceClient>(
            SnapshotBlobDefaults.BlobServiceClientServiceKey,
            (
                _,
                _
            ) => new(blobConnectionString));
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        return services.AddBlobSnapshotStorageProvider();
    }

    /// <summary>
    ///     Applies the provided options configuration and registers the Blob snapshot storage provider.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configureOptions">The options configuration action.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddBlobSnapshotStorageProvider(
        this IServiceCollection services,
        Action<SnapshotBlobStorageOptions> configureOptions
    )
    {
        services.Configure(configureOptions);
        return services.AddBlobSnapshotStorageProvider();
    }

    /// <summary>
    ///     Binds options from configuration and registers the Blob snapshot storage provider.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configuration">The configuration section containing Blob snapshot storage settings.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddBlobSnapshotStorageProvider(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<SnapshotBlobStorageOptions>(configuration);
        return services.AddBlobSnapshotStorageProvider();
    }

    /// <summary>
    ///     Creates the keyed <see cref="BlobServiceClient" />, binds options from configuration, and registers the provider.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="blobConnectionString">The Azure Blob Storage connection string.</param>
    /// <param name="configuration">The configuration section containing Blob snapshot storage settings.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddBlobSnapshotStorageProvider(
        this IServiceCollection services,
        string blobConnectionString,
        IConfiguration configuration
    )
    {
        services.AddKeyedSingleton<BlobServiceClient>(
            SnapshotBlobDefaults.BlobServiceClientServiceKey,
            (
                _,
                _
            ) => new(blobConnectionString));
        services.Configure<SnapshotBlobStorageOptions>(configuration);
        return services.AddBlobSnapshotStorageProvider();
    }
}