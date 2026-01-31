namespace Mississippi.EventSourcing.Brooks.Cosmos;

/// <summary>
///     Configuration options for the Cosmos DB brook storage provider.
/// </summary>
public sealed class BrookStorageOptions
{
    /// <summary>
    ///     Gets or sets the Cosmos DB container identifier for storing brooks.
    /// </summary>
    public string ContainerId { get; set; } = "brooks";

    /// <summary>
    ///     Gets or sets the keyed service key used to resolve the <c>CosmosClient</c> from DI.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Defaults to <c>"mississippi-cosmos-brooks-client"</c>.
    ///         Override this to share a single <c>CosmosClient</c> across multiple storage providers
    ///         or to use a custom keyed registration.
    ///     </para>
    /// </remarks>
    public string CosmosClientServiceKey { get; set; } = "mississippi-cosmos-brooks-client";

    /// <summary>
    ///     Gets or sets the Cosmos DB database identifier.
    /// </summary>
    public string DatabaseId { get; set; } = "mississippi";

    /// <summary>
    ///     Gets or sets the duration in seconds for lease expiration.
    /// </summary>
    public int LeaseDurationSeconds { get; set; } = 60;

    /// <summary>
    ///     Gets or sets the threshold in seconds for lease renewal operations.
    /// </summary>
    public int LeaseRenewalThresholdSeconds { get; set; } = 20;

    /// <summary>
    ///     Gets or sets the name of the container used for distributed locking.
    /// </summary>
    public string LockContainerName { get; set; } = "locks";

    /// <summary>
    ///     Gets or sets the maximum number of events per batch operation.
    /// </summary>
    // Default below 100 to respect Cosmos transactional batch operation limit
    public int MaxEventsPerBatch { get; set; } = 90;

    /// <summary>
    ///     Gets or sets the maximum request size in bytes for Cosmos DB operations.
    /// </summary>
    // Keep under 2MB server-side limit, allow some headroom
    public long MaxRequestSizeBytes { get; set; } = 1_700_000;

    /// <summary>
    ///     Gets or sets the batch size for query operations.
    /// </summary>
    public int QueryBatchSize { get; set; } = 100;
}