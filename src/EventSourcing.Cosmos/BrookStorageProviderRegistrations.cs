using Microsoft.Extensions.DependencyInjection;
using Mississippi.Core.Abstractions.Mapping;
using Mississippi.Core.Abstractions.Providers.Storage;
using Mississippi.Core.Abstractions.Streams;
using Mississippi.EventSourcing.Cosmos.Abstractions;
using Mississippi.EventSourcing.Cosmos.Storage;
using Mississippi.EventSourcing.Cosmos.Locking;
using Mississippi.EventSourcing.Cosmos.Batching;
using Mississippi.EventSourcing.Cosmos.Retry;
using Mississippi.EventSourcing.Cosmos.Streams;
using Mississippi.EventSourcing.Cosmos.Mapping;

namespace Mississippi.EventSourcing.Cosmos;

public static class BrookStorageProviderRegistrations
{
    public static IServiceCollection AddCosmosBrookStorageProvider(this IServiceCollection services)
    {
        services.AddScoped<IBrookStorageProvider, BrookStorageProvider>();

        services.AddScoped<IStreamRecoveryService, StreamRecoveryService>();
        services.AddScoped<IEventStreamReader, EventStreamReader>();
        services.AddScoped<IEventStreamAppender, EventStreamAppender>();
        
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
    
    public static IServiceCollection AddCosmosBrookStorageProvider<TEventFromStorageMapper, TEventToStorageMapper>(
        this IServiceCollection services)
        where TEventFromStorageMapper : class, IMapper<EventStorageModel, BrookEvent>
        where TEventToStorageMapper : class, IMapper<BrookEvent, EventStorageModel>
    {
        services.AddScoped<IBrookStorageProvider, BrookStorageProvider>();
        
        services.AddScoped<IStreamRecoveryService, StreamRecoveryService>();
        services.AddScoped<IEventStreamReader, EventStreamReader>();
        services.AddScoped<IEventStreamAppender, EventStreamAppender>();
        
        services.AddScoped<ICosmosRepository, CosmosRepository>();
        
        services.AddScoped<IDistributedLockManager, BlobDistributedLockManager>();
        
        services.AddSingleton<IBatchSizeEstimator, BatchSizeEstimator>();
        
        services.AddSingleton<IRetryPolicy, CosmosRetryPolicy>();
        
        services.AddMapper<EventStorageModel, BrookEvent, TEventFromStorageMapper>();
        services.AddMapper<BrookEvent, EventStorageModel, TEventToStorageMapper>();
        
        services.AddMapper<HeadDocument, HeadStorageModel, HeadDocumentToStorageMapper>();
        services.AddMapper<EventDocument, EventStorageModel, EventDocumentToStorageMapper>();
        
        return services;
    }
}
