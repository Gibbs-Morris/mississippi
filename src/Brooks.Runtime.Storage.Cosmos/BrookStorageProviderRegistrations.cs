using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Blobs;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
using Mississippi.Hosting.Abstractions;
using Mississippi.Hosting.Runtime.Abstractions;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos;

/// <summary>
///     Extension methods for composing Cosmos DB brook storage with the runtime builder.
/// </summary>
public static class BrookStorageProviderRegistrations
{
    private static ConditionalWeakTable<IRuntimeBuilder, object> Registrations { get; } = new();

    /// <summary>
    ///     Queues Cosmos DB brook storage using host-owned keyed SDK clients.
    /// </summary>
    /// <param name="builder">The runtime composition builder.</param>
    /// <param name="configure">Optional configuration of the nested Cosmos storage scope.</param>
    /// <returns>The runtime builder for chaining.</returns>
    /// <remarks>
    ///     The host must provide a keyed <see cref="CosmosClient" /> using the final
    ///     <see cref="CosmosBrookStorageBuilder.CosmosClientServiceKey" /> and a keyed
    ///     <see cref="BlobServiceClient" /> using <see cref="BrookCosmosDefaults.BlobLockingServiceKey" />.
    /// </remarks>
    public static IRuntimeBuilder AddCosmosBrookStorageProvider(
        this IRuntimeBuilder builder,
        Action<CosmosBrookStorageBuilder>? configure = null
    ) =>
        AddCore(builder, configure, null, null, null);

    /// <summary>
    ///     Queues Cosmos DB brook storage with lazily-created keyed SDK clients.
    /// </summary>
    /// <param name="builder">The runtime composition builder.</param>
    /// <param name="cosmosConnectionString">The Cosmos DB connection string.</param>
    /// <param name="blobStorageConnectionString">The Azure Blob Storage connection string.</param>
    /// <param name="configure">Optional configuration of the nested Cosmos storage scope.</param>
    /// <returns>The runtime builder for chaining.</returns>
    public static IRuntimeBuilder AddCosmosBrookStorageProvider(
        this IRuntimeBuilder builder,
        string cosmosConnectionString,
        string blobStorageConnectionString,
        Action<CosmosBrookStorageBuilder>? configure = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cosmosConnectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobStorageConnectionString);
        return AddCore(builder, configure, null, cosmosConnectionString, blobStorageConnectionString);
    }

    /// <summary>
    ///     Queues Cosmos DB brook storage using host-owned keyed SDK clients and configuration binding.
    /// </summary>
    /// <param name="builder">The runtime composition builder.</param>
    /// <param name="configuration">The configuration section containing BrookStorageOptions property names.</param>
    /// <returns>The runtime builder for chaining.</returns>
    /// <remarks>
    ///     The host must provide a keyed <see cref="CosmosClient" /> using the final configured client key and a keyed
    ///     <see cref="BlobServiceClient" /> using <see cref="BrookCosmosDefaults.BlobLockingServiceKey" />.
    /// </remarks>
    public static IRuntimeBuilder AddCosmosBrookStorageProvider(
        this IRuntimeBuilder builder,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return AddCore(builder, null, configuration, null, null);
    }

    /// <summary>
    ///     Queues Cosmos DB brook storage with lazily-created keyed SDK clients and configuration binding.
    /// </summary>
    /// <param name="builder">The runtime composition builder.</param>
    /// <param name="cosmosConnectionString">The Cosmos DB connection string.</param>
    /// <param name="blobStorageConnectionString">The Azure Blob Storage connection string.</param>
    /// <param name="configuration">The configuration section containing BrookStorageOptions property names.</param>
    /// <returns>The runtime builder for chaining.</returns>
    public static IRuntimeBuilder AddCosmosBrookStorageProvider(
        this IRuntimeBuilder builder,
        string cosmosConnectionString,
        string blobStorageConnectionString,
        IConfiguration configuration
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cosmosConnectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobStorageConnectionString);
        ArgumentNullException.ThrowIfNull(configuration);
        return AddCore(builder, null, configuration, cosmosConnectionString, blobStorageConnectionString);
    }

    /// <summary>
    ///     Registers the validated provider graph into the staged silo services.
    /// </summary>
    /// <param name="services">The staged service collection.</param>
    /// <param name="snapshot">The final validated options snapshot.</param>
    /// <param name="cosmosConnectionString">The optional owned Cosmos connection string.</param>
    /// <param name="blobStorageConnectionString">The optional owned Blob connection string.</param>
    internal static void RegisterCore(
        IServiceCollection services,
        BrookStorageOptions snapshot,
        string? cosmosConnectionString,
        string? blobStorageConnectionString
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(snapshot);
        if (cosmosConnectionString is not null || blobStorageConnectionString is not null)
        {
            services.AddKeyedSingleton<CosmosClient>(
                snapshot.CosmosClientServiceKey,
                (_, _) => new(cosmosConnectionString!));
            services.AddKeyedSingleton<BlobServiceClient>(
                BrookCosmosDefaults.BlobLockingServiceKey,
                (_, _) => new(blobStorageConnectionString!));
        }

        services.AddOptions<BrookStorageOptions>()
            .Configure(options => CopyOptions(snapshot, options))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<BrookStorageOptions>, BrookStorageOptionsValidator>();
        services.AddSingleton<IBrookStorageProvider, BrookStorageProvider>();
        services.AddSingleton<IBrookRecoveryService, BrookRecoveryService>();
        services.AddSingleton<IEventBrookReader, EventBrookReader>();
        services.AddSingleton<IEventBrookWriter, EventBrookWriter>();
        services.AddSingleton<ICosmosRepository, CosmosRepository>();

        // BlobDistributedLockManager uses [FromKeyedServices(BrookCosmosDefaults.BlobLockingServiceKey)] for BlobServiceClient.
        services.AddSingleton<IDistributedLockManager, BlobDistributedLockManager>();
        services.AddSingleton<IBlobLeaseClientFactory, BlobLeaseClientFactory>();
        services.AddSingleton<IBatchSizeEstimator, BatchSizeEstimator>();
        services.AddSingleton<IRetryPolicy, CosmosRetryPolicy>();
        services.AddMapper<EventStorageModel, BrookEvent, EventStorageToEventMapper>();
        services.AddMapper<BrookEvent, EventStorageModel, EventToStorageMapper>();
        services.AddMapper<CursorDocument, CursorStorageModel, CursorDocumentToStorageMapper>();
        services.AddMapper<EventDocument, EventStorageModel, EventDocumentToStorageMapper>();

        // Keep the three independent provider, reader, and writer descriptors exposed by the original registration.
        services.AddSingleton<IBrookStorageReader, BrookStorageProvider>();
        services.AddSingleton<IBrookStorageWriter, BrookStorageProvider>();
        services.AddHostedService<CosmosContainerInitializer>();
        services.AddKeyedSingleton<Container>(
            BrookCosmosDefaults.CosmosContainerServiceKey,
            (provider, _) =>
            {
                BrookStorageOptions options = provider.GetRequiredService<IOptions<BrookStorageOptions>>().Value;
                CosmosClient cosmosClient =
                    provider.GetRequiredKeyedService<CosmosClient>(options.CosmosClientServiceKey);
                Database database = cosmosClient.GetDatabase(options.DatabaseId);
                return database.GetContainer(options.ContainerId);
            });
    }

    private static IRuntimeBuilder AddCore(
        IRuntimeBuilder builder,
        Action<CosmosBrookStorageBuilder>? configure,
        IConfiguration? configuration,
        string? cosmosConnectionString,
        string? blobStorageConnectionString
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        IReadOnlyList<BuilderDiagnostic> diagnostics = builder.Validate();
        if (diagnostics.Count > 0)
        {
            throw new BuilderValidationException(diagnostics);
        }

        if (!Registrations.TryAdd(builder, new()))
        {
            throw new BuilderValidationException(
            [
                new(
                    BrookStorageBuilderDiagnosticCodes.DuplicateComposition,
                    "Cosmos Brooks storage has already been configured for this runtime.",
                    "Combine Brooks Cosmos settings in one AddCosmosBrookStorageProvider(...) callback."),
            ]);
        }

        try
        {
            builder.ConfigureSilo(silo =>
            {
                CosmosBrookStorageBuilder cosmos = new(cosmosConnectionString, blobStorageConnectionString);
                try
                {
                    if (configuration is not null)
                    {
                        cosmos.Bind(configuration);
                    }

                    configure?.Invoke(cosmos);
                    cosmos.Apply(silo);
                }
                finally
                {
                    cosmos.Close();
                }
            });
            return builder;
        }
        catch
        {
            Registrations.Remove(builder);
            throw;
        }
    }

    private static void CopyOptions(
        BrookStorageOptions source,
        BrookStorageOptions target
    )
    {
        target.ContainerId = source.ContainerId;
        target.CosmosClientServiceKey = source.CosmosClientServiceKey;
        target.DatabaseId = source.DatabaseId;
        target.LeaseDurationSeconds = source.LeaseDurationSeconds;
        target.LeaseRenewalThresholdSeconds = source.LeaseRenewalThresholdSeconds;
        target.LockContainerName = source.LockContainerName;
        target.MaxEventsPerBatch = source.MaxEventsPerBatch;
        target.MaxRequestSizeBytes = source.MaxRequestSizeBytes;
        target.QueryBatchSize = source.QueryBatchSize;
    }

    // Performs asynchronous Cosmos resource initialization without synchronous waits in DI.
    private sealed class CosmosContainerInitializer : IHostedService
    {
        [SuppressMessage(
            "Major Code Smell",
            "S1144:Unused private members should be removed",
            Justification = "Constructed via DI reflection")]
        public CosmosContainerInitializer(
            IServiceProvider serviceProvider,
            IOptions<BrookStorageOptions> options
        )
        {
            ServiceProvider = serviceProvider;
            Options = options;
        }

        private IOptions<BrookStorageOptions> Options { get; }

        private IServiceProvider ServiceProvider { get; }

        public async Task StartAsync(
            CancellationToken cancellationToken
        )
        {
            BrookStorageOptions options = Options.Value;
            CosmosClient cosmosClient =
                ServiceProvider.GetRequiredKeyedService<CosmosClient>(options.CosmosClientServiceKey);
            DatabaseResponse databaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(
                options.DatabaseId,
                cancellationToken: cancellationToken);
            Database database = databaseResponse.Database;
            try
            {
                Container existingContainer = database.GetContainer(options.ContainerId);
                ContainerResponse containerProperties =
                    await existingContainer.ReadContainerAsync(cancellationToken: cancellationToken);
                if (containerProperties.Resource.PartitionKeyPath != "/brookPartitionKey")
                {
                    throw new InvalidOperationException(
                        $"Existing Cosmos container '{options.ContainerId}' has partition key path '{containerProperties.Resource.PartitionKeyPath}', but '/brookPartitionKey' is required. Refuse to delete existing container. Please provision a container with the correct partition key.");
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Container doesn't exist, proceed to create.
            }

            await database.CreateContainerIfNotExistsAsync(
                options.ContainerId,
                "/brookPartitionKey",
                cancellationToken: cancellationToken);
        }

        public Task StopAsync(
            CancellationToken cancellationToken
        ) =>
            Task.CompletedTask;
    }
}