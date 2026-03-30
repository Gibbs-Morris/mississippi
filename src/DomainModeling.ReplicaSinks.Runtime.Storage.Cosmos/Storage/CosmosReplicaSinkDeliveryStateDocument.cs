using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

using Newtonsoft.Json;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.Storage;

/// <summary>
///     Cosmos document that stores durable latest-state delivery progress.
/// </summary>
internal sealed class CosmosReplicaSinkDeliveryStateDocument
{
    /// <summary>
    ///     Creates a Cosmos document from the supplied durable delivery state.
    /// </summary>
    /// <param name="state">The durable delivery state.</param>
    /// <returns>The corresponding Cosmos document.</returns>
    public static CosmosReplicaSinkDeliveryStateDocument FromDomain(
        ReplicaSinkDeliveryState state
    ) => new()
    {
        BootstrapUpperBoundSourcePosition = state.BootstrapUpperBoundSourcePosition,
        CommittedSourcePosition = state.CommittedSourcePosition,
        DeadLetter = CosmosReplicaSinkStoredFailureDocument.FromDomain(state.DeadLetter),
        DeliveryKey = state.DeliveryKey,
        DesiredSourcePosition = state.DesiredSourcePosition,
        Id = CosmosReplicaSinkDocumentKeys.DeliveryStateId(state.DeliveryKey),
        ReplicaPartitionKey = ReplicaSinkCosmosDefaults.DeliveryStatePartitionKey,
        Retry = CosmosReplicaSinkStoredFailureDocument.FromDomain(state.Retry),
        Type = CosmosReplicaSinkDocumentKeys.DeliveryStateDocumentType,
    };

    /// <summary>
    ///     Converts the Cosmos document into the durable delivery-state abstraction.
    /// </summary>
    /// <returns>The durable delivery state abstraction.</returns>
    public ReplicaSinkDeliveryState ToDomain() => new(
        DeliveryKey,
        DesiredSourcePosition,
        BootstrapUpperBoundSourcePosition,
        CommittedSourcePosition,
        Retry?.ToDomain(),
        DeadLetter?.ToDomain());

    /// <summary>
    ///     Gets or sets the bootstrap cutover fence for the lane.
    /// </summary>
    [JsonProperty("bootstrapUpperBoundSourcePosition")]
    public long? BootstrapUpperBoundSourcePosition { get; set; }

    /// <summary>
    ///     Gets or sets the highest durably committed source position.
    /// </summary>
    [JsonProperty("committedSourcePosition")]
    public long? CommittedSourcePosition { get; set; }

    /// <summary>
    ///     Gets or sets the currently persisted dead-letter state.
    /// </summary>
    [JsonProperty("deadLetter")]
    public CosmosReplicaSinkStoredFailureDocument? DeadLetter { get; set; }

    /// <summary>
    ///     Gets or sets the runtime-owned delivery key.
    /// </summary>
    [JsonProperty("deliveryKey")]
    public string DeliveryKey { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the highest desired source position observed for the lane.
    /// </summary>
    [JsonProperty("desiredSourcePosition")]
    public long? DesiredSourcePosition { get; set; }

    /// <summary>
    ///     Gets or sets the document identifier.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the partition key for the durable delivery-state partition.
    /// </summary>
    [JsonProperty("replicaPartitionKey")]
    public string ReplicaPartitionKey { get; set; } = ReplicaSinkCosmosDefaults.DeliveryStatePartitionKey;

    /// <summary>
    ///     Gets or sets the currently persisted retry state.
    /// </summary>
    [JsonProperty("retry")]
    public CosmosReplicaSinkStoredFailureDocument? Retry { get; set; }

    /// <summary>
    ///     Gets or sets the document discriminator.
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = CosmosReplicaSinkDocumentKeys.DeliveryStateDocumentType;
}
