using Azure.Storage.Blobs;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        services.AddSingleton<IBrookStorageProvider, BrookStorageProvider>();
        services.AddSingleton<IBrookRecoveryService, BrookRecoveryService>();
        services.AddSingleton<IEventBrookReader, EventBrookReader>();
        services.AddSingleton<IEventBrookAppender, EventBrookAppender>();
        services.AddSingleton<ICosmosRepository, CosmosRepository>();
        services.AddSingleton<IDistributedLockManager, BlobDistributedLockManager>();
        services.AddSingleton<IBatchSizeEstimator, BatchSizeEstimator>();
        services.AddSingleton<IRetryPolicy, CosmosRetryPolicy>();
        services.AddMapper<EventStorageModel, BrookEvent, EventStorageToEventMapper>();
        services.AddMapper<BrookEvent, EventStorageModel, EventToStorageMapper>();
        services.AddMapper<HeadDocument, HeadStorageModel, HeadDocumentToStorageMapper>();
        services.AddMapper<EventDocument, EventStorageModel, EventDocumentToStorageMapper>();

        // Register storage provider interfaces using the helper
        services.RegisterBrookStorageProvider<BrookStorageProvider>();

        // Configure Cosmos DB Container factory
        services.AddSingleton<Container>(provider =>
        {
            CosmosClient cosmosClient = provider.GetRequiredService<CosmosClient>();
            BrookStorageOptions options = provider.GetRequiredService<IOptions<BrookStorageOptions>>().Value;
            
            // Create database if it doesn't exist
            Database database = cosmosClient.CreateDatabaseIfNotExistsAsync(options.DatabaseId).GetAwaiter().GetResult();
            
            // Check if container exists and has the correct partition key path
            try
            {
                Container existingContainer = database.GetContainer(options.ContainerId);
                var containerProperties = existingContainer.ReadContainerAsync().GetAwaiter().GetResult();
                
                // If the partition key path is not what we expect, delete and recreate
                if (containerProperties.Resource.PartitionKeyPath != "/brookPartitionKey")
                {
                    Console.WriteLine($"DEBUG: Deleting existing container with wrong partition key path: {containerProperties.Resource.PartitionKeyPath}");
                    existingContainer.DeleteContainerAsync().GetAwaiter().GetResult();
                    Console.WriteLine($"DEBUG: Container deleted successfully");
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Container doesn't exist, which is fine
            }
            
            // Create container if it doesn't exist
            Container container = database.CreateContainerIfNotExistsAsync(options.ContainerId, "/brookPartitionKey").GetAwaiter().GetResult();
            
            return container;
        });
        return services;
    }

    /// <summary>
    ///     Adds Cosmos DB brook storage provider services to the service collection with configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="cosmosConnectionString">The Cosmos DB connection string.</param>
    /// <param name="blobStorageConnectionString">The Azure Blob Storage connection string for distributed locking.</param>
    /// <param name="configureOptions">Action to configure the BrookStorageOptions.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddCosmosBrookStorageProvider(
        this IServiceCollection services,
        string cosmosConnectionString,
        string blobStorageConnectionString,
        Action<BrookStorageOptions>? configureOptions = null
    )
    {
        // Register CosmosClient
        services.AddSingleton<CosmosClient>(_ => new(cosmosConnectionString));

        // Register BlobServiceClient for distributed locking
        services.AddSingleton<BlobServiceClient>(_ => new(blobStorageConnectionString));

        // Configure options if provided
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        return services.AddCosmosBrookStorageProvider();
    }

    /// <summary>
    ///     Adds Cosmos DB brook storage provider services to the service collection with configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">Action to configure the BrookStorageOptions.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddCosmosBrookStorageProvider(
        this IServiceCollection services,
        Action<BrookStorageOptions> configureOptions
    )
    {
        services.Configure(configureOptions);
        return services.AddCosmosBrookStorageProvider();
    }

    /// <summary>
    ///     Adds Cosmos DB brook storage provider services to the service collection with configuration from appsettings.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="cosmosConnectionString">The Cosmos DB connection string.</param>
    /// <param name="blobStorageConnectionString">The Azure Blob Storage connection string for distributed locking.</param>
    /// <param name="configuration">The configuration section containing BrookStorageOptions.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddCosmosBrookStorageProvider(
        this IServiceCollection services,
        string cosmosConnectionString,
        string blobStorageConnectionString,
        IConfiguration configuration
    )
    {
        // Register CosmosClient
        services.AddSingleton<CosmosClient>(_ => new(cosmosConnectionString));

        // Register BlobServiceClient for distributed locking
        services.AddSingleton<BlobServiceClient>(_ => new(blobStorageConnectionString));
        services.Configure<BrookStorageOptions>(configuration);
        return services.AddCosmosBrookStorageProvider();
    }

    /// <summary>
    ///     Adds Cosmos DB brook storage provider services to the service collection with configuration from appsettings.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The configuration section containing BrookStorageOptions.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddCosmosBrookStorageProvider(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<BrookStorageOptions>(configuration);
        return services.AddCosmosBrookStorageProvider();
    }
}