namespace Mississippi.EventSourcing.Brooks.Cosmos;

/// <summary>
///     Configuration options for the Cosmos DB brook storage provider.
/// </summary>
public class BrookStorageOptions
{
    /// <summary>
    ///     Gets the Cosmos DB container identifier for storing brooks.
    /// </summary>
    public string ContainerId { get; } = "brooks";

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
    public string LockContainerName { get; set; } = "brook-locks";

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