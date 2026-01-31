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
    ///     Gets or sets the keyed service key used to resolve the <c>CosmosClient</c> from DI.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Defaults to <c>"mississippi-cosmos-snapshots-client"</c>.
    ///         Override this to share a single <c>CosmosClient</c> across multiple storage providers
    ///         or to use a custom keyed registration.
    ///     </para>
    /// </remarks>
    public string CosmosClientServiceKey { get; set; } = "mississippi-cosmos-snapshots-client";

    /// <summary>
    ///     Gets or sets the Cosmos database identifier.
    /// </summary>
    public string DatabaseId { get; set; } = "mississippi";

    /// <summary>
    ///     Gets or sets the batch size for snapshot queries.
    /// </summary>
    public int QueryBatchSize { get; set; } = 100;
}