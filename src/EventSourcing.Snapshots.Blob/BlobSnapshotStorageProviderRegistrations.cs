using System;

using Azure.Storage.Blobs;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.Common.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Blob.Compression;
using Mississippi.EventSourcing.Snapshots.Blob.Storage;


namespace Mississippi.EventSourcing.Snapshots.Blob;

/// <summary>
///     Extension methods for registering Azure Blob snapshot storage provider services.
/// </summary>
public static class BlobSnapshotStorageProviderRegistrations
{
    /// <summary>
    ///     Registers Azure Blob snapshot storage provider services using an externally provided
    ///     <see cref="BlobServiceClient" /> and previously configured <see cref="BlobSnapshotStorageOptions" />;
    ///     ensures the container initializer runs at startup.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddBlobSnapshotStorageProvider(
        this IServiceCollection services
    )
    {
        // Register compression factories
        services.AddSingleton<NoCompressionCompressor>();
        services.AddSingleton<GZipSnapshotCompressor>();
        services.AddSingleton<BrotliSnapshotCompressor>();

        // Register compressor based on options
        services.AddSingleton<ISnapshotCompressor>(provider =>
        {
            BlobSnapshotStorageOptions options =
                provider.GetRequiredService<IOptions<BlobSnapshotStorageOptions>>().Value;
            return options.WriteCompression switch
            {
                SnapshotCompression.GZip => provider.GetRequiredService<GZipSnapshotCompressor>(),
                SnapshotCompression.Brotli => provider.GetRequiredService<BrotliSnapshotCompressor>(),
                var _ => provider.GetRequiredService<NoCompressionCompressor>(),
            };
        });

        // Register blob operations - keyed container client
        services.AddKeyedSingleton<BlobContainerClient>(
            MississippiDefaults.ServiceKeys.BlobSnapshots,
            (
                provider,
                _
            ) =>
            {
                BlobSnapshotStorageOptions options =
                    provider.GetRequiredService<IOptions<BlobSnapshotStorageOptions>>().Value;
                BlobServiceClient blobServiceClient =
                    provider.GetRequiredKeyedService<BlobServiceClient>(options.BlobServiceClientKey);
                return blobServiceClient.GetBlobContainerClient(options.ContainerName);
            });
        services.AddSingleton<IBlobSnapshotOperations, BlobSnapshotOperations>();
        services.AddSingleton<IBlobSnapshotRepository, BlobSnapshotRepository>();

        // Register the snapshot storage provider
        services.RegisterSnapshotStorageProvider<BlobSnapshotStorageProvider>();

        // Ensure container exists asynchronously on host start
        services.AddHostedService<BlobContainerInitializer>();
        return services;
    }

    /// <summary>
    ///     Creates a keyed <see cref="BlobServiceClient" /> from the supplied connection string and registers the
    ///     Azure Blob snapshot storage provider.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="blobConnectionString">Azure Blob Storage connection string used for client creation.</param>
    /// <param name="configureOptions">Optional options configuration applied during registration.</param>
    /// <returns>The service collection configured with a keyed Blob service client.</returns>
    public static IServiceCollection AddBlobSnapshotStorageProvider(
        this IServiceCollection services,
        string blobConnectionString,
        Action<BlobSnapshotStorageOptions>? configureOptions = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobConnectionString);

        // Register keyed BlobServiceClient for Snapshots storage
        services.AddKeyedSingleton<BlobServiceClient>(
            MississippiDefaults.ServiceKeys.BlobSnapshotsClient,
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
    ///     Applies the provided options configuration delegate and registers the Azure Blob snapshot storage provider
    ///     using an existing <see cref="BlobServiceClient" /> in DI.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configureOptions">Options configuration action applied before registration.</param>
    /// <returns>The service collection with configured snapshot storage options.</returns>
    public static IServiceCollection AddBlobSnapshotStorageProvider(
        this IServiceCollection services,
        Action<BlobSnapshotStorageOptions> configureOptions
    )
    {
        ArgumentNullException.ThrowIfNull(configureOptions);
        services.Configure(configureOptions);
        return services.AddBlobSnapshotStorageProvider();
    }

    /// <summary>
    ///     Binds <see cref="BlobSnapshotStorageOptions" /> from configuration and registers the Azure Blob snapshot
    ///     storage provider that relies on an external <see cref="BlobServiceClient" />.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configuration">Configuration section containing snapshot storage settings.</param>
    /// <returns>The service collection with bound snapshot storage options.</returns>
    public static IServiceCollection AddBlobSnapshotStorageProvider(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.Configure<BlobSnapshotStorageOptions>(configuration);
        return services.AddBlobSnapshotStorageProvider();
    }
}