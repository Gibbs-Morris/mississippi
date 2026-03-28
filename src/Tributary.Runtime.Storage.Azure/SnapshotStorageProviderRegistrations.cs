using System;

using Azure.Storage.Blobs;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.Tributary.Runtime.Storage.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Azure.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Azure
{
    /// <summary>
    ///     Extension methods for registering Azure Blob Storage Tributary snapshot provider services.
    /// </summary>
    public static class SnapshotStorageProviderRegistrations
    {
        /// <summary>
        ///     Adds the Azure Blob Storage Tributary snapshot provider using an externally registered keyed <see cref="BlobServiceClient" />.
        /// </summary>
        /// <param name="services">The service collection to update.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddAzureSnapshotStorageProvider(
            this IServiceCollection services
        )
        {
            ArgumentNullException.ThrowIfNull(services);

            _ = services.AddOptions<SnapshotStorageOptions>().ValidateOnStart();
            _ = services.AddSingleton<IValidateOptions<SnapshotStorageOptions>, SnapshotStorageOptionsValidator>();
            _ = services.AddSingleton<ISnapshotPathEncoder, Sha256SnapshotPathEncoder>();
            _ = services.AddSingleton<ISnapshotDocumentCodec, AzureSnapshotDocumentCodec>();
            _ = services.AddSingleton<IAzureSnapshotRepository>(serviceProvider =>
            {
                IOptions<SnapshotStorageOptions> options = serviceProvider.GetRequiredService<IOptions<SnapshotStorageOptions>>();
                BlobServiceClient blobServiceClient = AzureBlobServiceClientResolver.Resolve(
                    serviceProvider,
                    options.Value.BlobServiceClientServiceKey);
                return new AzureSnapshotRepository(
                    blobServiceClient,
                    options,
                    serviceProvider.GetRequiredService<ISnapshotPathEncoder>(),
                    serviceProvider.GetRequiredService<ISnapshotDocumentCodec>());
            });
            _ = services.RegisterSnapshotStorageProvider<SnapshotStorageProvider>();
            _ = services.AddHostedService<AzureSnapshotStorageInitializer>();
            return services;
        }

        /// <summary>
        ///     Adds the Azure Blob Storage Tributary snapshot provider and applies the supplied options delegate.
        /// </summary>
        /// <param name="services">The service collection to update.</param>
        /// <param name="configureOptions">The options configuration delegate.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddAzureSnapshotStorageProvider(
            this IServiceCollection services,
            Action<SnapshotStorageOptions> configureOptions
        )
        {
            ArgumentNullException.ThrowIfNull(configureOptions);
            _ = services.Configure(configureOptions);
            return services.AddAzureSnapshotStorageProvider();
        }

        /// <summary>
        ///     Adds the Azure Blob Storage Tributary snapshot provider and binds <see cref="SnapshotStorageOptions" /> from configuration.
        /// </summary>
        /// <param name="services">The service collection to update.</param>
        /// <param name="configurationSection">The configuration section to bind.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddAzureSnapshotStorageProvider(
            this IServiceCollection services,
            IConfiguration configurationSection
        )
        {
            ArgumentNullException.ThrowIfNull(configurationSection);
            _ = services.Configure<SnapshotStorageOptions>(configurationSection);
            return services.AddAzureSnapshotStorageProvider();
        }

        /// <summary>
        ///     Adds the Azure Blob Storage Tributary snapshot provider and registers a keyed <see cref="BlobServiceClient" /> from the supplied connection string.
        /// </summary>
        /// <param name="services">The service collection to update.</param>
        /// <param name="connectionString">The Azure Blob Storage connection string used to create the keyed client.</param>
        /// <param name="configureOptions">Optional options configuration applied before the client registration is finalized.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddAzureSnapshotStorageProvider(
            this IServiceCollection services,
            string connectionString,
            Action<SnapshotStorageOptions>? configureOptions = null
        )
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            SnapshotStorageOptions configuredOptions = new();
            configureOptions?.Invoke(configuredOptions);

            _ = services.AddKeyedSingleton<BlobServiceClient>(
                configuredOptions.BlobServiceClientServiceKey,
                (
                    _,
                    _
                ) => new(connectionString));

            if (configureOptions != null)
            {
                _ = services.Configure(configureOptions);
            }

            return services.AddAzureSnapshotStorageProvider();
        }
    }
}
