namespace Mississippi.EventSourcing.Cosmos;

/// <summary>
/// Configuration options for the Cosmos DB brook storage provider.
/// </summary>
public class BrookStorageOptions
{
    /// <summary>
    /// Gets or sets the Cosmos DB database identifier.
    /// </summary>
    public string DatabaseId { get; set; } = "mississippi";

    /// <summary>
    /// Gets the Cosmos DB container identifier for storing brooks.
    /// </summary>
    public string ContainerId { get; } = "brooks";

    /// <summary>
    /// Gets or sets the name of the container used for distributed locking.
    /// </summary>
    public string LockContainerName { get; set; } = "stream-locks";

    /// <summary>
    /// Gets or sets the batch size for query operations.
    /// </summary>
    public int QueryBatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum number of events per batch operation.
    /// </summary>
    public int MaxEventsPerBatch { get; set; } = 95;

    /// <summary>
    /// Gets or sets the duration in seconds for lease expiration.
    /// </summary>
    public int LeaseDurationSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the maximum request size in bytes for Cosmos DB operations.
    /// </summary>
    public long MaxRequestSizeBytes { get; set; } = 1_800_000;

    /// <summary>
    /// Gets or sets the threshold in seconds for lease renewal operations.
    /// </summary>
    public int LeaseRenewalThresholdSeconds { get; set; } = 20;
}