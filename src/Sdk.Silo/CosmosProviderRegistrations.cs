using System;

using Azure.Storage.Blobs;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Mississippi.Common.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Streaming;
using Mississippi.EventSourcing.Brooks.Cosmos;
using Mississippi.EventSourcing.Snapshots.Cosmos;


namespace Mississippi.Sdk.Silo;

/// <summary>
///     Cosmos provider registrations for Orleans silo hosts.
/// </summary>
public static class CosmosProviderRegistrations
{
    /// <summary>
    ///     Configures Cosmos-backed storage providers and Orleans streaming defaults.
    /// </summary>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <param name="configure">Optional action to configure Cosmos provider options.</param>
    /// <returns>The Mississippi silo builder for chaining.</returns>
    public static MississippiSiloBuilder AddCosmosProviders(
        this MississippiSiloBuilder builder,
        Action<MississippiCosmosOptions>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        MississippiCosmosOptions options = new();
        configure?.Invoke(options);
        builder.HostBuilder.AddAzureCosmosClient(options.ConnectionName);
        builder.HostBuilder.AddKeyedAzureBlobServiceClient(options.BlobConnectionName);
        builder.Services.AddKeyedSingleton(
            MississippiDefaults.ServiceKeys.BlobLocking,
            (
                sp,
                _
            ) => sp.GetRequiredKeyedService<BlobServiceClient>(options.BlobConnectionName));
        builder.Services.AddKeyedSingleton(
            MississippiDefaults.ServiceKeys.CosmosBrooksClient,
            (
                sp,
                _
            ) => sp.GetRequiredService<CosmosClient>());
        builder.Services.AddKeyedSingleton(
            MississippiDefaults.ServiceKeys.CosmosSnapshotsClient,
            (
                sp,
                _
            ) => sp.GetRequiredService<CosmosClient>());
        string brooksContainerId = string.Concat(options.ContainerPrefix, MississippiDefaults.ContainerIds.Brooks);
        string snapshotsContainerId = string.Concat(
            options.ContainerPrefix,
            MississippiDefaults.ContainerIds.Snapshots);
        string lockContainerId = string.Concat(options.ContainerPrefix, MississippiDefaults.ContainerIds.Locks);
        builder.Services.AddOptions<BrookStorageOptions>();
        builder.Services.Configure<BrookStorageOptions>(o =>
        {
            o.DatabaseId = options.DatabaseId;
            o.ContainerId = brooksContainerId;
            o.LockContainerName = lockContainerId;
            o.CosmosClientServiceKey = MississippiDefaults.ServiceKeys.CosmosBrooksClient;
        });
        builder.Services.AddOptions<SnapshotStorageOptions>();
        builder.Services.Configure<SnapshotStorageOptions>(o =>
        {
            o.DatabaseId = options.DatabaseId;
            o.ContainerId = snapshotsContainerId;
            o.CosmosClientServiceKey = MississippiDefaults.ServiceKeys.CosmosSnapshotsClient;
        });
        builder.Services.AddOptions<BrookProviderOptions>();
        builder.Services.Configure<BrookProviderOptions>(o => o.OrleansStreamProviderName = options.StreamProviderName);
        builder.Services.AddCosmosBrookStorageProvider();
        builder.Services.AddCosmosSnapshotStorageProvider();
        return builder;
    }
}