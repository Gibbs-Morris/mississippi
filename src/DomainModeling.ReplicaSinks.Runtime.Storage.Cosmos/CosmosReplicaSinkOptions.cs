using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos;

/// <summary>
///     Defines configuration for a named Cosmos-backed replica sink registration.
/// </summary>
public sealed class CosmosReplicaSinkOptions : ReplicaSinkRegistrationOptions
{
    /// <summary>
    ///     Gets or sets the Cosmos container identifier used for replica sink documents.
    /// </summary>
    public string ContainerId { get; set; } = ReplicaSinkCosmosDefaults.ContainerId;

    /// <summary>
    ///     Gets or sets the Cosmos database identifier used for replica sink documents.
    /// </summary>
    public string DatabaseId { get; set; } = ReplicaSinkCosmosDefaults.DatabaseId;

    /// <summary>
    ///     Gets or sets the maximum page size used for Cosmos queries.
    /// </summary>
    public int QueryBatchSize { get; set; } = ReplicaSinkCosmosDefaults.DefaultQueryBatchSize;
}
