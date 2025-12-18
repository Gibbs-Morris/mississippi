using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Blobs;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        services.AddSingleton<IBlobLeaseClientFactory, BlobLeaseClientFactory>();
        services.AddSingleton<IBatchSizeEstimator, BatchSizeEstimator>();
        services.AddSingleton<IRetryPolicy, CosmosRetryPolicy>();
        services.AddMapper<EventStorageModel, BrookEvent, EventStorageToEventMapper>();
        services.AddMapper<BrookEvent, EventStorageModel, EventToStorageMapper>();
        services.AddMapper<CursorDocument, CursorStorageModel, CursorDocumentToStorageMapper>();
        services.AddMapper<EventDocument, EventStorageModel, EventDocumentToStorageMapper>();

        // Register storage provider interfaces using the helper
        services.RegisterBrookStorageProvider<BrookStorageProvider>();

        // Ensure Cosmos DB resources are created asynchronously on host start
        services.AddHostedService<CosmosContainerInitializer>();

        // Configure Cosmos DB Container factory using keyed service to avoid conflicts with other Cosmos providers
        services.AddKeyedSingleton<Container>(
            CosmosContainerKeys.Brooks,
            (
                provider,
                _
            ) =>
            {
                CosmosClient cosmosClient = provider.GetRequiredService<CosmosClient>();
                BrookStorageOptions options = provider.GetRequiredService<IOptions<BrookStorageOptions>>().Value;

                // Return a handle; CosmosContainerInitializer will ensure existence on startup
                Database database = cosmosClient.GetDatabase(options.DatabaseId);
                return database.GetContainer(options.ContainerId);
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

    // Performs asynchronous Cosmos resource initialization without synchronous waits in DI
    private sealed class CosmosContainerInitializer : IHostedService
    {
        [SuppressMessage(
            "Major Code Smell",
            "S1144:Unused private members should be removed",
            Justification = "Constructed via DI reflection")]
        public CosmosContainerInitializer(
            CosmosClient cosmosClient,
            IOptions<BrookStorageOptions> options
        )
        {
            CosmosClient = cosmosClient;
            Options = options;
        }

        private CosmosClient CosmosClient { get; }

        private IOptions<BrookStorageOptions> Options { get; }

        public async Task StartAsync(
            CancellationToken cancellationToken
        )
        {
            BrookStorageOptions o = Options.Value;

            // Ensure database exists
            DatabaseResponse dbResponse = await CosmosClient.CreateDatabaseIfNotExistsAsync(
                o.DatabaseId,
                cancellationToken: cancellationToken);
            Database database = dbResponse.Database;

            // Validate existing container partition key path if present
            try
            {
                Container existingContainer = database.GetContainer(o.ContainerId);
                ContainerResponse containerProperties =
                    await existingContainer.ReadContainerAsync(cancellationToken: cancellationToken);
                if (containerProperties.Resource.PartitionKeyPath != "/brookPartitionKey")
                {
                    throw new InvalidOperationException(
                        $"Existing Cosmos container '{o.ContainerId}' has partition key path '{containerProperties.Resource.PartitionKeyPath}', but '/brookPartitionKey' is required. Refuse to delete existing container. Please provision a container with the correct partition key.");
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Container doesn't exist, proceed to create
            }

            // Ensure container exists
            await database.CreateContainerIfNotExistsAsync(
                o.ContainerId,
                "/brookPartitionKey",
                cancellationToken: cancellationToken);
        }

        public Task StopAsync(
            CancellationToken cancellationToken
        ) =>
            Task.CompletedTask;
    }
}