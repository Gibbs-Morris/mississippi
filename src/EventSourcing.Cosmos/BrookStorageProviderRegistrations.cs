using Microsoft.Extensions.DependencyInjection;

using Mississippi.Core.Abstractions.Mapping;
using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Abstractions.Storage;
using Mississippi.EventSourcing.Cosmos.Abstractions;
using Mississippi.EventSourcing.Cosmos.Batching;
using Mississippi.EventSourcing.Cosmos.Brooks;
using Mississippi.EventSourcing.Cosmos.Locking;
using Mississippi.EventSourcing.Cosmos.Mapping;
using Mississippi.EventSourcing.Cosmos.Retry;
using Mississippi.EventSourcing.Cosmos.Storage;


namespace Mississippi.EventSourcing.Cosmos;

/// <summary>
///     Extension methods for registering Cosmos DB brook storage provider services.
/// </summary>
public static class BrookStorageProviderRegistrations
{
    /// <summary>
    ///     Adds Cosmos DB brook storage provider services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddCosmosBrookStorageProvider(
        this IServiceCollection services
    )
    {
        services.AddScoped<IBrookStorageProvider, BrookStorageProvider>();
        services.AddScoped<IBrookRecoveryService, BrookRecoveryService>();
        services.AddScoped<IEventBrookReader, EventBrookReader>();
        services.AddScoped<IEventBrookAppender, EventBrookAppender>();
        services.AddScoped<ICosmosRepository, CosmosRepository>();
        services.AddScoped<IDistributedLockManager, BlobDistributedLockManager>();
        services.AddSingleton<IBatchSizeEstimator, BatchSizeEstimator>();
        services.AddSingleton<IRetryPolicy, CosmosRetryPolicy>();
        services.AddMapper<EventStorageModel, BrookEvent, EventStorageToEventMapper>();
        services.AddMapper<BrookEvent, EventStorageModel, EventToStorageMapper>();
        services.AddMapper<HeadDocument, HeadStorageModel, HeadDocumentToStorageMapper>();
        services.AddMapper<EventDocument, EventStorageModel, EventDocumentToStorageMapper>();
        return services;
    }
}