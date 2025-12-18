using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.Core.Abstractions.Mapping;
using Mississippi.Core.Cosmos.Retry;
using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Cosmos.Abstractions;
using Mississippi.EventSourcing.Snapshots.Cosmos.Mapping;
using Mississippi.EventSourcing.Snapshots.Cosmos.Storage;


namespace Mississippi.EventSourcing.Snapshots.Cosmos;

/// <summary>
///     Extension methods for registering Cosmos snapshot storage provider services.
/// </summary>
public static class SnapshotStorageProviderRegistrations
{
    /// <summary>
    ///     Registers Cosmos snapshot storage provider services using an externally provided <see cref="CosmosClient" /> and
    ///     previously configured <see cref="SnapshotStorageOptions" />; ensures the container initializer runs at startup.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddCosmosSnapshotStorageProvider(
        this IServiceCollection services
    )
    {
        // Register container operations abstraction (single point of Cosmos SDK contact)
        services.AddSingleton<ISnapshotContainerOperations, SnapshotContainerOperations>();
        services.AddSingleton<ISnapshotCosmosRepository, SnapshotCosmosRepository>();
        services.AddSingleton<IRetryPolicy, CosmosRetryPolicy>();
        services.AddMapper<SnapshotDocument, SnapshotStorageModel, SnapshotDocumentToStorageMapper>();
        services.AddMapper<SnapshotStorageModel, SnapshotEnvelope, SnapshotStorageToEnvelopeMapper>();
        services.AddMapper<SnapshotWriteModel, SnapshotStorageModel, SnapshotWriteModelToStorageMapper>();
        services.AddMapper<SnapshotStorageModel, SnapshotDocument, SnapshotStorageToDocumentMapper>();
        services.AddMapper<SnapshotDocument, SnapshotEnvelope, SnapshotDocumentToEnvelopeMapper>();
        services.RegisterSnapshotStorageProvider<SnapshotStorageProvider>();

        // Ensure container exists asynchronously on host start
        services.AddHostedService<CosmosContainerInitializer>();

        // Provide container handle using keyed service to avoid conflicts with other Cosmos providers
        services.AddKeyedSingleton<Container>(
            CosmosContainerKeys.Snapshots,
            (
                provider,
                _
            ) =>
            {
                CosmosClient client = provider.GetRequiredService<CosmosClient>();
                SnapshotStorageOptions options = provider.GetRequiredService<IOptions<SnapshotStorageOptions>>().Value;
                Database database = client.GetDatabase(options.DatabaseId);
                return database.GetContainer(options.ContainerId);
            });
        return services;
    }

    /// <summary>
    ///     Creates a <see cref="CosmosClient" /> from the supplied connection string and registers the Cosmos snapshot storage
    ///     provider.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="cosmosConnectionString">Cosmos connection string used for client creation.</param>
    /// <param name="configureOptions">Optional options configuration applied during registration.</param>
    /// <returns>The service collection configured with a Cosmos client.</returns>
    public static IServiceCollection AddCosmosSnapshotStorageProvider(
        this IServiceCollection services,
        string cosmosConnectionString,
        Action<SnapshotStorageOptions>? configureOptions = null
    )
    {
        services.AddSingleton<CosmosClient>(_ => new(cosmosConnectionString));
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        return services.AddCosmosSnapshotStorageProvider();
    }

    /// <summary>
    ///     Applies the provided options configuration delegate and registers the Cosmos snapshot storage provider using an
    ///     existing <see cref="CosmosClient" /> in DI.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configureOptions">Options configuration action applied before registration.</param>
    /// <returns>The service collection with configured snapshot storage options.</returns>
    public static IServiceCollection AddCosmosSnapshotStorageProvider(
        this IServiceCollection services,
        Action<SnapshotStorageOptions> configureOptions
    )
    {
        services.Configure(configureOptions);
        return services.AddCosmosSnapshotStorageProvider();
    }

    /// <summary>
    ///     Binds <see cref="SnapshotStorageOptions" /> from configuration and registers the Cosmos snapshot storage provider
    ///     that relies on an external <see cref="CosmosClient" />.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configuration">Configuration section containing snapshot storage settings.</param>
    /// <returns>The service collection with bound snapshot storage options.</returns>
    public static IServiceCollection AddCosmosSnapshotStorageProvider(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<SnapshotStorageOptions>(configuration);
        return services.AddCosmosSnapshotStorageProvider();
    }

    private sealed class CosmosContainerInitializer : IHostedService
    {
        private const string PartitionKeyPath = "/snapshotPartitionKey";

        private readonly CosmosClient cosmosClient;

        private readonly IOptions<SnapshotStorageOptions> options;

        [SuppressMessage("Major Code Smell", "S1144", Justification = "Used by DI")]
        public CosmosContainerInitializer(
            CosmosClient cosmosClient,
            IOptions<SnapshotStorageOptions> options
        )
        {
            this.cosmosClient = cosmosClient;
            this.options = options;
        }

        public async Task StartAsync(
            CancellationToken cancellationToken
        )
        {
            SnapshotStorageOptions o = options.Value;
            DatabaseResponse db = await cosmosClient.CreateDatabaseIfNotExistsAsync(
                o.DatabaseId,
                cancellationToken: cancellationToken);
            Database database = db.Database;
            try
            {
                ContainerResponse container = await database.GetContainer(o.ContainerId)
                    .ReadContainerAsync(cancellationToken: cancellationToken);
                if (!string.Equals(container.Resource.PartitionKeyPath, PartitionKeyPath, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Existing Cosmos container '{o.ContainerId}' has partition key path '{container.Resource.PartitionKeyPath}', but '{PartitionKeyPath}' is required.");
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Container will be created below
            }

            await database.CreateContainerIfNotExistsAsync(
                o.ContainerId,
                PartitionKeyPath,
                cancellationToken: cancellationToken);
        }

        public Task StopAsync(
            CancellationToken cancellationToken
        ) =>
            Task.CompletedTask;
    }
}