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

using Mississippi.Common.Abstractions;
using Mississippi.Common.Abstractions.Builders;
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Common.Cosmos.Abstractions.Retry;
using Mississippi.Common.Cosmos.Retry;
using Mississippi.EventSourcing.Snapshots.Abstractions;
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
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <returns>The updated builder.</returns>
    public static IMississippiSiloBuilder AddCosmosSnapshotStorageProvider(
        this IMississippiSiloBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(AddCosmosSnapshotStorageProviderServices);
        return builder;
    }

    /// <summary>
    ///     Creates a keyed <see cref="CosmosClient" /> from the supplied connection string and registers the Cosmos snapshot
    ///     storage
    ///     provider.
    /// </summary>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <param name="cosmosConnectionString">Cosmos connection string used for client creation.</param>
    /// <param name="configureOptions">Optional options configuration applied during registration.</param>
    /// <returns>The builder configured with a keyed Cosmos client.</returns>
    public static IMississippiSiloBuilder AddCosmosSnapshotStorageProvider(
        this IMississippiSiloBuilder builder,
        string cosmosConnectionString,
        Action<SnapshotStorageOptions>? configureOptions = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services =>
        {
            // Register keyed CosmosClient for Snapshots storage
            services.AddKeyedSingleton<CosmosClient>(
                MississippiDefaults.ServiceKeys.CosmosSnapshotsClient,
                (
                    _,
                    _
                ) => new(cosmosConnectionString));
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
        });
        return builder.AddCosmosSnapshotStorageProvider();
    }

    /// <summary>
    ///     Applies the provided options configuration delegate and registers the Cosmos snapshot storage provider using an
    ///     existing <see cref="CosmosClient" /> in DI.
    /// </summary>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <param name="configureOptions">Options configuration action applied before registration.</param>
    /// <returns>The builder with configured snapshot storage options.</returns>
    public static IMississippiSiloBuilder AddCosmosSnapshotStorageProvider(
        this IMississippiSiloBuilder builder,
        Action<SnapshotStorageOptions> configureOptions
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services => services.Configure(configureOptions));
        return builder.AddCosmosSnapshotStorageProvider();
    }

    /// <summary>
    ///     Binds <see cref="SnapshotStorageOptions" /> from configuration and registers the Cosmos snapshot storage provider
    ///     that relies on an external <see cref="CosmosClient" />.
    /// </summary>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <param name="configuration">Configuration section containing snapshot storage settings.</param>
    /// <returns>The builder with bound snapshot storage options.</returns>
    public static IMississippiSiloBuilder AddCosmosSnapshotStorageProvider(
        this IMississippiSiloBuilder builder,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services => services.Configure<SnapshotStorageOptions>(configuration));
        return builder.AddCosmosSnapshotStorageProvider();
    }

    private static void AddCosmosSnapshotStorageProviderServices(
        IServiceCollection services
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

        // Provide container handle using keyed services to avoid conflicts with other Cosmos providers
        // Uses CosmosClientServiceKey from options (defaults to MississippiDefaults.ServiceKeys.CosmosSnapshotsClient)
        services.AddKeyedSingleton<Container>(
            MississippiDefaults.ServiceKeys.CosmosSnapshots,
            (
                provider,
                _
            ) =>
            {
                SnapshotStorageOptions options = provider.GetRequiredService<IOptions<SnapshotStorageOptions>>().Value;
                CosmosClient client = provider.GetRequiredKeyedService<CosmosClient>(options.CosmosClientServiceKey);
                Database database = client.GetDatabase(options.DatabaseId);
                return database.GetContainer(options.ContainerId);
            });
    }

    private sealed class CosmosContainerInitializer : IHostedService
    {
        private const string PartitionKeyPath = "/snapshotPartitionKey";

        [SuppressMessage("Major Code Smell", "S1144", Justification = "Used by DI")]
        public CosmosContainerInitializer(
            IServiceProvider serviceProvider,
            IOptions<SnapshotStorageOptions> options
        )
        {
            ServiceProvider = serviceProvider;
            Options = options;
        }

        private IOptions<SnapshotStorageOptions> Options { get; }

        private IServiceProvider ServiceProvider { get; }

        public async Task StartAsync(
            CancellationToken cancellationToken
        )
        {
            SnapshotStorageOptions o = Options.Value;
            CosmosClient cosmosClient = ServiceProvider.GetRequiredKeyedService<CosmosClient>(o.CosmosClientServiceKey);
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