using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Blobs;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.Common.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Blob.Storage;


namespace Mississippi.EventSourcing.Snapshots.Blob;

/// <summary>
///     Extension methods for registering Azure Blob snapshot storage provider services.
/// </summary>
public static class SnapshotStorageProviderRegistrations
{
    /// <summary>
    ///     Registers Azure Blob snapshot storage provider services using an externally provided
    ///     <see cref="BlobServiceClient" /> and
    ///     previously configured <see cref="SnapshotStorageOptions" />; ensures the container initializer runs at startup.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddBlobSnapshotStorageProvider(
        this IServiceCollection services
    )
    {
        // Register blob operations abstraction (single point of Azure SDK contact)
        services.AddSingleton<IBlobSnapshotOperations, BlobSnapshotOperations>();
        services.RegisterSnapshotStorageProvider<SnapshotStorageProvider>();

        // Ensure container exists asynchronously on host start
        services.AddHostedService<BlobContainerInitializer>();

        // Provide container handle using keyed services to avoid conflicts with other Blob providers
        // Uses BlobServiceClientKey from options (defaults to MississippiDefaults.ServiceKeys.BlobSnapshotsClient)
        services.AddSingleton<BlobContainerClient>(provider =>
        {
            SnapshotStorageOptions options = provider.GetRequiredService<IOptions<SnapshotStorageOptions>>().Value;
            BlobServiceClient client =
                provider.GetRequiredKeyedService<BlobServiceClient>(options.BlobServiceClientKey);
            return client.GetBlobContainerClient(options.ContainerId);
        });
        return services;
    }

    /// <summary>
    ///     Creates a keyed <see cref="BlobServiceClient" /> from the supplied connection string and registers the Blob
    ///     snapshot
    ///     storage provider.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="blobConnectionString">Blob connection string used for client creation.</param>
    /// <param name="configureOptions">Optional options configuration applied during registration.</param>
    /// <returns>The service collection configured with a keyed Blob client.</returns>
    public static IServiceCollection AddBlobSnapshotStorageProvider(
        this IServiceCollection services,
        string blobConnectionString,
        Action<SnapshotStorageOptions>? configureOptions = null
    )
    {
        // Register keyed BlobServiceClient for Snapshots storage
        services.AddKeyedSingleton<BlobServiceClient>(
            MississippiDefaults.ServiceKeys.BlobSnapshotsClient,
            (
                _,
                _
            ) => new(blobConnectionString));
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        return services.AddBlobSnapshotStorageProvider();
    }

    /// <summary>
    ///     Applies the provided options configuration delegate and registers the Blob snapshot storage provider using an
    ///     existing <see cref="BlobServiceClient" /> in DI.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configureOptions">Options configuration action applied before registration.</param>
    /// <returns>The service collection with configured snapshot storage options.</returns>
    public static IServiceCollection AddBlobSnapshotStorageProvider(
        this IServiceCollection services,
        Action<SnapshotStorageOptions> configureOptions
    )
    {
        services.Configure(configureOptions);
        return services.AddBlobSnapshotStorageProvider();
    }

    /// <summary>
    ///     Binds <see cref="SnapshotStorageOptions" /> from configuration and registers the Blob snapshot storage provider
    ///     that relies on an external <see cref="BlobServiceClient" />.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configuration">Configuration section containing snapshot storage settings.</param>
    /// <returns>The service collection with bound snapshot storage options.</returns>
    public static IServiceCollection AddBlobSnapshotStorageProvider(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<SnapshotStorageOptions>(configuration);
        return services.AddBlobSnapshotStorageProvider();
    }

    private sealed class BlobContainerInitializer : IHostedService
    {
        [SuppressMessage("Major Code Smell", "S1144", Justification = "Used by DI")]
        public BlobContainerInitializer(
            IServiceProvider serviceProvider,
            IOptions<SnapshotStorageOptions> options
        )
        {
            ServiceProvider = serviceProvider;
            Options = options;
        }

        private IOptions<SnapshotStorageOptions> Options { get; }

        private IServiceProvider ServiceProvider { get; }

        public async Task StartAsync(
            CancellationToken cancellationToken
        )
        {
            SnapshotStorageOptions o = Options.Value;
            BlobServiceClient blobServiceClient =
                ServiceProvider.GetRequiredKeyedService<BlobServiceClient>(o.BlobServiceClientKey);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(o.ContainerId);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        }

        public Task StopAsync(
            CancellationToken cancellationToken
        ) =>
            Task.CompletedTask;
    }
}