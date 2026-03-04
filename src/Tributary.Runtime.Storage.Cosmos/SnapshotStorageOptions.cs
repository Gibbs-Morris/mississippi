namespace Mississippi.Tributary.Runtime.Storage.Cosmos;

/// <summary>
///     Options controlling Cosmos snapshot persistence.
/// </summary>
public sealed class SnapshotStorageOptions
{
    /// <summary>
    ///     Gets or sets the Cosmos container identifier for snapshots.
    /// </summary>
    public string ContainerId { get; set; } = SnapshotCosmosDefaults.ContainerId;

    /// <summary>
    ///     Gets or sets the keyed service key used to resolve the <c>CosmosClient</c> from DI.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Defaults to <see cref="SnapshotCosmosDefaults.CosmosClientServiceKey" />.
    ///         Override this to share a single <c>CosmosClient</c> across multiple storage providers
    ///         or to use a custom keyed registration.
    ///     </para>
    /// </remarks>
    public string CosmosClientServiceKey { get; set; } = SnapshotCosmosDefaults.CosmosClientServiceKey;

    /// <summary>
    ///     Gets or sets the Cosmos database identifier.
    /// </summary>
    public string DatabaseId { get; set; } = SnapshotCosmosDefaults.DatabaseId;

    /// <summary>
    ///     Gets or sets the batch size for snapshot queries.
    /// </summary>
    public int QueryBatchSize { get; set; } = 100;
}