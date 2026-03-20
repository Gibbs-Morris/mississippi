using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Common.Runtime.Storage.Abstractions.Retry;
using Mississippi.Common.Runtime.Storage.Cosmos.Retry;
using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Cosmos.Mapping;
using Mississippi.Tributary.Runtime.Storage.Cosmos.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Cosmos;

/// <summary>
///     Non-obsolete internal registration entry point for Cosmos Snapshot storage services.
///     Used by <c>MississippiRuntimeBuilder.AddCosmosSnapshotStorage</c>.
/// </summary>
internal static class SnapshotCosmosStorageRegistration
{
    /// <summary>
    ///     Registers all Cosmos Snapshot storage services into the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    internal static void RegisterServices(
        IServiceCollection services
    )
    {
        services.AddSingleton<ISnapshotContainerOperations, SnapshotContainerOperations>();
        services.AddSingleton<ISnapshotCosmosRepository, SnapshotCosmosRepository>();
        services.AddSingleton<IRetryPolicy, CosmosRetryPolicy>();
        services.AddTransient<IMapper<SnapshotDocument, SnapshotStorageModel>, SnapshotDocumentToStorageMapper>();
        services.AddTransient<IMapper<SnapshotStorageModel, SnapshotEnvelope>, SnapshotStorageToEnvelopeMapper>();
        services.AddTransient<IMapper<SnapshotWriteModel, SnapshotStorageModel>, SnapshotWriteModelToStorageMapper>();
        services.AddTransient<IMapper<SnapshotStorageModel, SnapshotDocument>, SnapshotStorageToDocumentMapper>();
        services.AddTransient<IMapper<SnapshotDocument, SnapshotEnvelope>, SnapshotDocumentToEnvelopeMapper>();

        // Inline RegisterSnapshotStorageProvider<SnapshotStorageProvider>()
        services.TryAddSingleton<ISnapshotStorageProvider, SnapshotStorageProvider>();
        services.AddSingleton<ISnapshotStorageReader>(provider =>
            provider.GetRequiredService<ISnapshotStorageProvider>());
        services.AddSingleton<ISnapshotStorageWriter>(provider =>
            provider.GetRequiredService<ISnapshotStorageProvider>());
        services.AddHostedService<SnapshotCosmosContainerInitializer>();
        services.AddKeyedSingleton<Container>(
            SnapshotCosmosDefaults.CosmosContainerServiceKey,
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
}