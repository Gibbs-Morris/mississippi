using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Blobs;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blob.Mapping;
using Mississippi.Tributary.Runtime.Storage.Blob.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Blob;

/// <summary>
///     Extension methods for registering Blob snapshot storage provider services.
/// </summary>
public static class SnapshotStorageProviderRegistrations
{
    /// <summary>
    ///     Registers Blob snapshot storage provider services using an externally provided <see cref="BlobServiceClient" />
    ///     and previously configured <see cref="SnapshotBlobStorageOptions" />.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddBlobSnapshotStorageProvider(
        this IServiceCollection services
    )
    {
        services.AddOptions<SnapshotBlobStorageOptions>()
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<SnapshotBlobStorageOptions>, SnapshotBlobStorageOptionsValidator>());
        services.AddSingleton<ISnapshotBlobContainerOperations, SnapshotBlobContainerOperations>();
        services.AddSingleton<ISnapshotBlobRepository, SnapshotBlobRepository>();
        services.AddMapper<SnapshotBlobStorageModel, SnapshotEnvelope, SnapshotStorageToEnvelopeMapper>();
        services.AddMapper<SnapshotWriteModel, SnapshotBlobStorageModel, SnapshotWriteModelToStorageMapper>();
        services.RegisterSnapshotStorageProvider<SnapshotStorageProvider>();
        services.AddHostedService<BlobContainerInitializer>();
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
    /// <param name="blobStorageConnectionString">Blob storage connection string used for client creation.</param>
    /// <param name="configureOptions">Optional options configuration applied during registration.</param>
    /// <returns>The service collection configured with a keyed Blob client.</returns>
    public static IServiceCollection AddBlobSnapshotStorageProvider(
        this IServiceCollection services,
        string blobStorageConnectionString,
        Action<SnapshotBlobStorageOptions>? configureOptions = null
    )
    {
        services.AddKeyedSingleton(
            SnapshotBlobDefaults.BlobServiceClientServiceKey,
            (
                _,
                _
            ) => new BlobServiceClient(blobStorageConnectionString));
        if (configureOptions is not null)
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
        Action<SnapshotBlobStorageOptions> configureOptions
    )
    {
        services.Configure(configureOptions);
        return services.AddBlobSnapshotStorageProvider();
    }

    /// <summary>
    ///     Binds <see cref="SnapshotBlobStorageOptions" /> from configuration and registers the Blob snapshot storage
    ///     provider that relies on an external <see cref="BlobServiceClient" />.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configuration">Configuration section containing snapshot storage settings.</param>
    /// <returns>The service collection with bound snapshot storage options.</returns>
    public static IServiceCollection AddBlobSnapshotStorageProvider(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<SnapshotBlobStorageOptions>(configuration);
        return services.AddBlobSnapshotStorageProvider();
    }

    private sealed class BlobContainerInitializer : IHostedService
    {
        [SuppressMessage(
            "Major Code Smell",
            "S1144:Unused private members should be removed",
            Justification = "Constructed via DI reflection")]
        public BlobContainerInitializer(
            ISnapshotBlobContainerOperations containerOperations
        ) =>
            ContainerOperations = containerOperations;

        private ISnapshotBlobContainerOperations ContainerOperations { get; }

        public Task StartAsync(
            CancellationToken cancellationToken
        ) =>
            ContainerOperations.EnsureContainerExistsAsync(cancellationToken);

        public Task StopAsync(
            CancellationToken cancellationToken
        ) =>
            Task.CompletedTask;
    }
}
