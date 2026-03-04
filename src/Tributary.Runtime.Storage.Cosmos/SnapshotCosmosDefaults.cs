namespace Mississippi.Tributary.Runtime.Storage.Cosmos;

/// <summary>
///     Default constants for snapshot Cosmos storage configuration and keyed service registrations.
/// </summary>
public static class SnapshotCosmosDefaults
{
    /// <summary>
    ///     The default Cosmos DB database identifier for snapshot storage.
    /// </summary>
    public const string DatabaseId = "mississippi";

    /// <summary>
    ///     The default Cosmos DB container identifier for snapshots.
    /// </summary>
    public const string ContainerId = "snapshots";

    /// <summary>
    ///     The keyed DI service key for the snapshot Cosmos container.
    /// </summary>
    public const string CosmosContainerServiceKey = "mississippi-cosmos-snapshots";

    /// <summary>
    ///     The keyed DI service key for the snapshot Cosmos client.
    /// </summary>
    public const string CosmosClientServiceKey = "mississippi-cosmos-snapshots-client";
}