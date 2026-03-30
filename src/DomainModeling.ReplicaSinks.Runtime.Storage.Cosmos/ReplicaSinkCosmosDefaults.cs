using System;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos;

/// <summary>
///     Defines the module-owned defaults for the Cosmos-backed replica sink provider.
/// </summary>
public static class ReplicaSinkCosmosDefaults
{
    /// <summary>
    ///     The default Cosmos container identifier used for replica sink documents.
    /// </summary>
    public const string ContainerId = "replica-sinks";

    /// <summary>
    ///     The default Cosmos database identifier used for replica sink documents.
    /// </summary>
    public const string DatabaseId = "mississippi";

    /// <summary>
    ///     The default batch size used for Cosmos queries.
    /// </summary>
    public const int DefaultQueryBatchSize = 100;

    /// <summary>
    ///     The informational provider format name exposed to replica sink runtime diagnostics.
    /// </summary>
    public const string FormatName = "cosmos-db";

    /// <summary>
    ///     The module-owned prefix used when generating keyed container registrations for named sinks.
    /// </summary>
    internal const string ContainerServiceKeyPrefix = "mississippi-cosmos-replica-sinks";

    /// <summary>
    ///     The partition key value used for aggregate durable delivery-state documents.
    /// </summary>
    internal const string DeliveryStatePartitionKey = "delivery-state";

    /// <summary>
    ///     The required Cosmos partition key path for replica sink containers.
    /// </summary>
    internal const string PartitionKeyPath = "/replicaPartitionKey";

    /// <summary>
    ///     Creates the keyed container registration name for a specific sink registration.
    /// </summary>
    /// <param name="sinkKey">The named sink registration key.</param>
    /// <returns>The keyed container registration name.</returns>
    internal static string CreateContainerServiceKey(
        string sinkKey
    )
    {
        ArgumentNullException.ThrowIfNull(sinkKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(sinkKey);
        return $"{ContainerServiceKeyPrefix}-{sinkKey}";
    }
}
