using Mississippi.Common.Abstractions;


namespace Mississippi.EventSourcing.Snapshots.Cosmos;

/// <summary>
///     Options controlling Cosmos snapshot persistence.
/// </summary>
public sealed class SnapshotStorageOptions
{
    /// <summary>
    ///     Gets or sets the Cosmos container identifier for snapshots.
    /// </summary>
    public string ContainerId { get; set; } = MississippiDefaults.ContainerIds.Snapshots;

    /// <summary>
    ///     Gets or sets the Cosmos database identifier.
    /// </summary>
    public string DatabaseId { get; set; } = MississippiDefaults.DatabaseId;

    /// <summary>
    ///     Gets or sets the batch size for snapshot queries.
    /// </summary>
    public int QueryBatchSize { get; set; } = 100;
}