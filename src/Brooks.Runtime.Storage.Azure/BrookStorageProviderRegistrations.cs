using System;

using Azure.Storage.Blobs;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Runtime.Storage.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Azure.Brooks;
using Mississippi.Brooks.Runtime.Storage.Azure.Locking;
using Mississippi.Brooks.Runtime.Storage.Azure.Storage;


namespace Mississippi.Brooks.Runtime.Storage.Azure;

/// <summary>
///     Extension methods for registering Azure Blob Storage Brooks provider services.
/// </summary>
public static class BrookStorageProviderRegistrations
{
    /// <summary>
    ///     Adds the Azure Blob Storage Brooks provider using an externally registered keyed <see cref="BlobServiceClient" />.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAzureBrookStorageProvider(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<BrookStorageOptions>().ValidateOnStart();
        services.AddSingleton<IValidateOptions<BrookStorageOptions>, BrookStorageOptionsValidator>();
        services.AddSingleton<IStreamPathEncoder, Sha256StreamPathEncoder>();
        services.AddSingleton<IAzureBrookEventDocumentCodec, AzureBrookEventDocumentCodec>();
        services.AddSingleton<IAzureBrookRepository>(serviceProvider =>
        {
            IOptions<BrookStorageOptions> options = serviceProvider.GetRequiredService<IOptions<BrookStorageOptions>>();
            BlobServiceClient blobServiceClient = AzureBlobServiceClientResolver.Resolve(
                serviceProvider,
                options.Value.BlobServiceClientServiceKey);
            return new AzureBrookRepository(
                blobServiceClient,
                options,
                serviceProvider.GetRequiredService<IStreamPathEncoder>(),
                serviceProvider.GetRequiredService<IAzureBrookEventDocumentCodec>());
        });
        services.AddSingleton<IDistributedLockManager>(serviceProvider =>
        {
            IOptions<BrookStorageOptions> options = serviceProvider.GetRequiredService<IOptions<BrookStorageOptions>>();
            BlobServiceClient blobServiceClient = AzureBlobServiceClientResolver.Resolve(
                serviceProvider,
                options.Value.BlobServiceClientServiceKey);
            return new BlobDistributedLockManager(
                blobServiceClient,
                options,
                serviceProvider.GetRequiredService<IStreamPathEncoder>());
        });
        services.AddSingleton<IBrookRecoveryService, BrookRecoveryService>();
        services.AddSingleton<IEventBrookWriter, EventBrookWriter>();
        services.AddSingleton<IBrookStorageProvider, BrookStorageProvider>();
        services.RegisterBrookStorageProvider<BrookStorageProvider>();
        services.AddHostedService<AzureBrookStorageInitializer>();
        return services;
    }

    /// <summary>
    ///     Adds the Azure Blob Storage Brooks provider and applies the supplied options delegate.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configureOptions">The options configuration delegate.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAzureBrookStorageProvider(
        this IServiceCollection services,
        Action<BrookStorageOptions> configureOptions
    )
    {
        ArgumentNullException.ThrowIfNull(configureOptions);
        services.Configure(configureOptions);
        return services.AddAzureBrookStorageProvider();
    }

    /// <summary>
    ///     Adds the Azure Blob Storage Brooks provider and binds <see cref="BrookStorageOptions" /> from configuration.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configurationSection">The configuration section to bind.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAzureBrookStorageProvider(
        this IServiceCollection services,
        IConfiguration configurationSection
    )
    {
        ArgumentNullException.ThrowIfNull(configurationSection);
        services.Configure<BrookStorageOptions>(configurationSection);
        return services.AddAzureBrookStorageProvider();
    }

    /// <summary>
    ///     Adds the Azure Blob Storage Brooks provider and registers a keyed <see cref="BlobServiceClient" /> from the
    ///     supplied connection string.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="connectionString">The Azure Blob Storage connection string used to create the keyed client.</param>
    /// <param name="configureOptions">Optional options configuration applied before the client registration is finalized.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAzureBrookStorageProvider(
        this IServiceCollection services,
        string connectionString,
        Action<BrookStorageOptions>? configureOptions = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        BrookStorageOptions configuredOptions = new();
        configureOptions?.Invoke(configuredOptions);

        services.AddKeyedSingleton<BlobServiceClient>(
            configuredOptions.BlobServiceClientServiceKey,
            (
                _,
                _
            ) => new BlobServiceClient(connectionString));

        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        return services.AddAzureBrookStorageProvider();
    }
}