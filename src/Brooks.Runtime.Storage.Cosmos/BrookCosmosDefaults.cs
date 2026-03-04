namespace Mississippi.Brooks.Runtime.Storage.Cosmos;

/// <summary>
///     Default constants for Brooks Cosmos storage configuration and keyed service registrations.
/// </summary>
public static class BrookCosmosDefaults
{
    /// <summary>
    ///     The keyed DI service key for the blob client used by distributed locking.
    /// </summary>
    public const string BlobLockingServiceKey = "mississippi-blob-locking";

    /// <summary>
    ///     The default Cosmos DB container identifier for Brooks event streams.
    /// </summary>
    public const string ContainerId = "brooks";

    /// <summary>
    ///     The keyed DI service key for the Brooks Cosmos client.
    /// </summary>
    public const string CosmosClientServiceKey = "mississippi-cosmos-brooks-client";

    /// <summary>
    ///     The keyed DI service key for the Brooks Cosmos container.
    /// </summary>
    public const string CosmosContainerServiceKey = "mississippi-cosmos-brooks";

    /// <summary>
    ///     The default Cosmos DB database identifier for Brooks storage.
    /// </summary>
    public const string DatabaseId = "mississippi";

    /// <summary>
    ///     The default Blob Storage container identifier for distributed locks.
    /// </summary>
    public const string LockContainerId = "locks";
}