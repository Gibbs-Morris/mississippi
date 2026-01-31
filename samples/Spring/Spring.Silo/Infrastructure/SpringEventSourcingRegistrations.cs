using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Brooks;
using Mississippi.EventSourcing.Brooks.Cosmos;
using Mississippi.EventSourcing.Serialization.Json;
using Mississippi.EventSourcing.Snapshots;
using Mississippi.EventSourcing.Snapshots.Cosmos;


namespace Spring.Silo.Infrastructure;

/// <summary>
///     Event sourcing infrastructure configuration (Brooks, Snapshots, Cosmos).
/// </summary>
internal static class SpringEventSourcingRegistrations
{
    private const string DatabaseId = "spring-db";

    private const string EventsContainerId = "events";

    private const string SnapshotsContainerId = "snapshots";

    /// <summary>
    ///     Adds event sourcing infrastructure with Cosmos storage providers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSpringEventSourcing(
        this IServiceCollection services
    )
    {
        AddSerializationAndCaching(services);
        AddBrooksCosmosProvider(services);
        AddSnapshotsCosmosProvider(services);
        return services;
    }

    private static void AddBrooksCosmosProvider(
        IServiceCollection services
    )
    {
        services.AddCosmosBrookStorageProvider(options =>
        {
            options.CosmosClientServiceKey = SpringAspireRegistrations.SharedCosmosServiceKey;
            options.DatabaseId = DatabaseId;
            options.ContainerId = EventsContainerId;
            options.QueryBatchSize = 50;
            options.MaxEventsPerBatch = 50;
        });
    }

    private static void AddSerializationAndCaching(
        IServiceCollection services
    )
    {
        services.AddJsonSerialization();
        services.AddEventSourcingByService();
        services.AddSnapshotCaching();
    }

    private static void AddSnapshotsCosmosProvider(
        IServiceCollection services
    )
    {
        services.AddCosmosSnapshotStorageProvider(options =>
        {
            options.CosmosClientServiceKey = SpringAspireRegistrations.SharedCosmosServiceKey;
            options.DatabaseId = DatabaseId;
            options.ContainerId = SnapshotsContainerId;
            options.QueryBatchSize = 100;
        });
    }
}