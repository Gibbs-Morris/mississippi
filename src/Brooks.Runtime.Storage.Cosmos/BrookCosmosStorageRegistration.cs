using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Batching;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Brooks;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Locking;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Mapping;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Storage;
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Common.Runtime.Storage.Abstractions.Retry;
using Mississippi.Common.Runtime.Storage.Cosmos.Retry;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos;

/// <summary>
///     Non-obsolete internal registration entry point for Cosmos Brook storage services.
///     Used by <c>MississippiRuntimeBuilder.AddCosmosEventStorage</c>.
/// </summary>
internal static class BrookCosmosStorageRegistration
{
    /// <summary>
    ///     Registers all Cosmos Brook storage services into the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    internal static void RegisterServices(
        IServiceCollection services
    )
    {
        services.AddSingleton<IBrookStorageProvider, BrookStorageProvider>();
        services.AddSingleton<IBrookRecoveryService, BrookRecoveryService>();
        services.AddSingleton<IEventBrookReader, EventBrookReader>();
        services.AddSingleton<IEventBrookWriter, EventBrookWriter>();
        services.AddSingleton<ICosmosRepository, CosmosRepository>();
        services.AddSingleton<IDistributedLockManager, BlobDistributedLockManager>();
        services.AddSingleton<IBlobLeaseClientFactory, BlobLeaseClientFactory>();
        services.AddSingleton<IBatchSizeEstimator, BatchSizeEstimator>();
        services.AddSingleton<IRetryPolicy, CosmosRetryPolicy>();
        services.AddTransient<IMapper<EventStorageModel, BrookEvent>, EventStorageToEventMapper>();
        services.AddTransient<IMapper<BrookEvent, EventStorageModel>, EventToStorageMapper>();
        services.AddTransient<IMapper<CursorDocument, CursorStorageModel>, CursorDocumentToStorageMapper>();
        services.AddTransient<IMapper<EventDocument, EventStorageModel>, EventDocumentToStorageMapper>();

        // Inline RegisterBrookStorageProvider<BrookStorageProvider>()
        services.AddSingleton<IBrookStorageWriter, BrookStorageProvider>();
        services.AddSingleton<IBrookStorageReader, BrookStorageProvider>();
        services.AddHostedService<BrookCosmosContainerInitializer>();
        services.AddKeyedSingleton<Container>(
            BrookCosmosDefaults.CosmosContainerServiceKey,
            (
                provider,
                _
            ) =>
            {
                BrookStorageOptions options = provider.GetRequiredService<IOptions<BrookStorageOptions>>().Value;
                CosmosClient cosmosClient =
                    provider.GetRequiredKeyedService<CosmosClient>(options.CosmosClientServiceKey);
                Database database = cosmosClient.GetDatabase(options.DatabaseId);
                return database.GetContainer(options.ContainerId);
            });
    }
}