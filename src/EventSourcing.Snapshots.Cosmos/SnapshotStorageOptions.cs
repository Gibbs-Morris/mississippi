namespace Mississippi.EventSourcing.Snapshots.Cosmos;

/// <summary>
///     Options controlling Cosmos snapshot persistence.
/// </summary>
public sealed class SnapshotStorageOptions
{
    /// <summary>
    ///     Gets or sets the Cosmos container identifier for snapshots.
    /// </summary>
    public string ContainerId { get; set; } = "snapshots";

    /// <summary>
    ///     Gets or sets the Cosmos database identifier.
    /// </summary>
    public string DatabaseId { get; set; } = "mississippi";

    /// <summary>
    ///     Gets or sets the batch size for snapshot queries.
    /// </summary>
    public int QueryBatchSize { get; set; } = 100;
}