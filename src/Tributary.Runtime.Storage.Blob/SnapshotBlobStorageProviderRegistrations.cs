using System;

using Azure.Storage.Blobs;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.Tributary.Runtime.Storage.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blob.Startup;


namespace Mississippi.Tributary.Runtime.Storage.Blob;

/// <summary>
///     Extension methods for registering Blob snapshot storage provider services.
/// </summary>
internal static class SnapshotBlobStorageProviderRegistrations
{
    /// <summary>
    ///     Registers Blob snapshot storage provider services using an externally provided keyed <see cref="BlobServiceClient" />
    ///     and previously configured <see cref="SnapshotBlobStorageOptions" />.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    internal static IServiceCollection AddBlobSnapshotStorageProvider(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<SnapshotBlobStorageOptions>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<SnapshotBlobStorageOptions>, SnapshotBlobStorageOptionsValidator>());
        services.AddSingleton<SnapshotPayloadSerializerResolver>();
        services.AddSingleton<IBlobContainerInitializerOperations, BlobContainerInitializerOperations>();
        services.RegisterSnapshotStorageProvider<SnapshotBlobStorageProvider>();
        services.AddHostedService<BlobContainerInitializer>();

        // Caller must register a keyed BlobServiceClient with options.BlobServiceClientServiceKey.
        services.AddKeyedSingleton<BlobContainerClient>(
            SnapshotBlobDefaults.BlobContainerServiceKey,
            (
                provider,
                _
            ) =>
            {
                SnapshotBlobStorageOptions options = provider.GetRequiredService<IOptions<SnapshotBlobStorageOptions>>().Value;
                BlobServiceClient blobServiceClient =
                    provider.GetRequiredKeyedService<BlobServiceClient>(options.BlobServiceClientServiceKey);
                return blobServiceClient.GetBlobContainerClient(options.ContainerName);
            });

        return services;
    }

    /// <summary>
    ///     Creates a keyed <see cref="BlobServiceClient" /> from the supplied connection string and registers the Blob
    ///     snapshot storage provider.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="blobConnectionString">Blob storage connection string used for client creation.</param>
    /// <param name="configureOptions">Optional options configuration applied during registration.</param>
    /// <returns>The configured service collection.</returns>
    internal static IServiceCollection AddBlobSnapshotStorageProvider(
        this IServiceCollection services,
        string blobConnectionString,
        Action<SnapshotBlobStorageOptions>? configureOptions = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobConnectionString);

        services.AddKeyedSingleton<BlobServiceClient>(
            SnapshotBlobDefaults.BlobServiceClientServiceKey,
            (
                _,
                _
            ) => new BlobServiceClient(blobConnectionString));

        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        return services.AddBlobSnapshotStorageProvider();
    }

    /// <summary>
    ///     Applies the provided options configuration delegate and registers the Blob snapshot storage provider.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configureOptions">Options configuration action applied before registration.</param>
    /// <returns>The configured service collection.</returns>
    internal static IServiceCollection AddBlobSnapshotStorageProvider(
        this IServiceCollection services,
        Action<SnapshotBlobStorageOptions> configureOptions
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);
        return services.AddBlobSnapshotStorageProvider();
    }

    /// <summary>
    ///     Binds <see cref="SnapshotBlobStorageOptions" /> from configuration and registers the Blob snapshot storage
    ///     provider that relies on an external keyed <see cref="BlobServiceClient" />.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configuration">Configuration section containing Blob snapshot storage settings.</param>
    /// <returns>The configured service collection.</returns>
    internal static IServiceCollection AddBlobSnapshotStorageProvider(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<SnapshotBlobStorageOptions>(configuration);
        return services.AddBlobSnapshotStorageProvider();
    }
}