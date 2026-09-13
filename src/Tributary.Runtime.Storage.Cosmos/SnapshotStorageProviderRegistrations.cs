using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Common.Runtime.Storage.Abstractions.Retry;
using Mississippi.Common.Runtime.Storage.Cosmos.Retry;
using Mississippi.Hosting.Abstractions;
using Mississippi.Hosting.Runtime.Abstractions;
using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Cosmos.Mapping;
using Mississippi.Tributary.Runtime.Storage.Cosmos.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Cosmos;

/// <summary>
///     Extension methods for composing Cosmos DB snapshot storage with the runtime builder.
/// </summary>
public static class SnapshotStorageProviderRegistrations
{
    private static ConditionalWeakTable<IRuntimeBuilder, object> Registrations { get; } = new();

    /// <summary>
    ///     Queues Cosmos DB snapshot storage using a host-owned keyed Cosmos client.
    /// </summary>
    /// <param name="builder">The runtime composition builder.</param>
    /// <param name="configure">Optional configuration of the nested Cosmos storage scope.</param>
    /// <returns>The runtime builder for chaining.</returns>
    /// <remarks>
    ///     The host must provide a keyed <see cref="CosmosClient" /> using the final
    ///     <see cref="CosmosSnapshotStorageBuilder.CosmosClientServiceKey" />.
    /// </remarks>
    public static IRuntimeBuilder AddCosmosSnapshotStorageProvider(
        this IRuntimeBuilder builder,
        Action<CosmosSnapshotStorageBuilder>? configure = null
    ) =>
        AddCore(builder, configure, null, null);

    /// <summary>
    ///     Queues Cosmos DB snapshot storage with a lazily-created keyed Cosmos client.
    /// </summary>
    /// <param name="builder">The runtime composition builder.</param>
    /// <param name="cosmosConnectionString">The Cosmos DB connection string.</param>
    /// <param name="configure">Optional configuration of the nested Cosmos storage scope.</param>
    /// <returns>The runtime builder for chaining.</returns>
    public static IRuntimeBuilder AddCosmosSnapshotStorageProvider(
        this IRuntimeBuilder builder,
        string cosmosConnectionString,
        Action<CosmosSnapshotStorageBuilder>? configure = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cosmosConnectionString);
        return AddCore(builder, configure, null, cosmosConnectionString);
    }

    /// <summary>
    ///     Queues Cosmos DB snapshot storage using a host-owned keyed Cosmos client and configuration binding.
    /// </summary>
    /// <param name="builder">The runtime composition builder.</param>
    /// <param name="configuration">The configuration containing SnapshotStorageOptions property names.</param>
    /// <returns>The runtime builder for chaining.</returns>
    /// <remarks>
    ///     The host must provide a keyed <see cref="CosmosClient" /> using the final configured client key.
    /// </remarks>
    public static IRuntimeBuilder AddCosmosSnapshotStorageProvider(
        this IRuntimeBuilder builder,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return AddCore(builder, null, configuration, null);
    }

    /// <summary>
    ///     Queues Cosmos DB snapshot storage with a lazily-created keyed Cosmos client and configuration binding.
    /// </summary>
    /// <param name="builder">The runtime composition builder.</param>
    /// <param name="cosmosConnectionString">The Cosmos DB connection string.</param>
    /// <param name="configuration">The configuration containing SnapshotStorageOptions property names.</param>
    /// <returns>The runtime builder for chaining.</returns>
    public static IRuntimeBuilder AddCosmosSnapshotStorageProvider(
        this IRuntimeBuilder builder,
        string cosmosConnectionString,
        IConfiguration configuration
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cosmosConnectionString);
        ArgumentNullException.ThrowIfNull(configuration);
        return AddCore(builder, null, configuration, cosmosConnectionString);
    }

    /// <summary>
    ///     Registers the validated Snapshot Cosmos graph into the staged silo services.
    /// </summary>
    /// <param name="services">The staged service collection.</param>
    /// <param name="snapshot">The final validated options snapshot.</param>
    /// <param name="cosmosConnectionString">The optional owned Cosmos connection string.</param>
    internal static void RegisterCore(
        IServiceCollection services,
        SnapshotStorageOptions snapshot,
        string? cosmosConnectionString
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(snapshot);
        if (cosmosConnectionString is not null)
        {
            services.AddKeyedSingleton<CosmosClient>(
                snapshot.CosmosClientServiceKey,
                (_, _) => new(cosmosConnectionString));
        }

        services.AddOptions<SnapshotStorageOptions>()
            .Configure(options => CopyOptions(snapshot, options))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SnapshotStorageOptions>, SnapshotStorageOptionsValidator>();
        services.AddSingleton<ISnapshotContainerOperations, SnapshotContainerOperations>();
        services.AddSingleton<ISnapshotCosmosRepository, SnapshotCosmosRepository>();
        services.AddSingleton<IRetryPolicy, CosmosRetryPolicy>();
        services.AddMapper<SnapshotDocument, SnapshotStorageModel, SnapshotDocumentToStorageMapper>();
        services.AddMapper<SnapshotStorageModel, SnapshotEnvelope, SnapshotStorageToEnvelopeMapper>();
        services.AddMapper<SnapshotWriteModel, SnapshotStorageModel, SnapshotWriteModelToStorageMapper>();
        services.AddMapper<SnapshotStorageModel, SnapshotDocument, SnapshotStorageToDocumentMapper>();
        services.AddMapper<SnapshotDocument, SnapshotEnvelope, SnapshotDocumentToEnvelopeMapper>();

        // Preserve the generic helper's shared provider identity while owning its descriptors here.
        services.TryAddSingleton<ISnapshotStorageProvider, SnapshotStorageProvider>();
        services.AddSingleton<ISnapshotStorageReader>(provider =>
            provider.GetRequiredService<ISnapshotStorageProvider>());
        services.AddSingleton<ISnapshotStorageWriter>(provider =>
            provider.GetRequiredService<ISnapshotStorageProvider>());
        services.AddHostedService<CosmosContainerInitializer>();
        services.AddKeyedSingleton<Container>(
            SnapshotCosmosDefaults.CosmosContainerServiceKey,
            (provider, _) =>
            {
                SnapshotStorageOptions options = provider.GetRequiredService<IOptions<SnapshotStorageOptions>>().Value;
                CosmosClient client = provider.GetRequiredKeyedService<CosmosClient>(options.CosmosClientServiceKey);
                Database database = client.GetDatabase(options.DatabaseId);
                return database.GetContainer(options.ContainerId);
            });
    }

    private static IRuntimeBuilder AddCore(
        IRuntimeBuilder builder,
        Action<CosmosSnapshotStorageBuilder>? configure,
        IConfiguration? configuration,
        string? cosmosConnectionString
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
                    SnapshotStorageBuilderDiagnosticCodes.DuplicateComposition,
                    "Cosmos Snapshot storage has already been configured for this runtime.",
                    "Combine Snapshot Cosmos settings in one AddCosmosSnapshotStorageProvider(...) callback."),
            ]);
        }

        try
        {
            builder.ConfigureSilo(silo =>
            {
                CosmosSnapshotStorageBuilder snapshot = new(cosmosConnectionString);
                try
                {
                    if (configuration is not null)
                    {
                        snapshot.Bind(configuration);
                    }

                    configure?.Invoke(snapshot);
                    snapshot.Apply(silo);
                }
                finally
                {
                    snapshot.Close();
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
        SnapshotStorageOptions source,
        SnapshotStorageOptions target
    )
    {
        target.ContainerId = source.ContainerId;
        target.CosmosClientServiceKey = source.CosmosClientServiceKey;
        target.DatabaseId = source.DatabaseId;
        target.QueryBatchSize = source.QueryBatchSize;
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