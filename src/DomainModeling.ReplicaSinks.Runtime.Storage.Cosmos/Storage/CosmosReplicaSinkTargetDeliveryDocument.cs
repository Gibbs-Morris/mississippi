using System;

using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

using Newtonsoft.Json;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.Storage;

/// <summary>
///     Cosmos document that stores the latest-state projection for a delivery lane.
/// </summary>
internal sealed class CosmosReplicaSinkTargetDeliveryDocument
{
    /// <summary>
    ///     Gets or sets the cumulative applied-write count for the lane.
    /// </summary>
    [JsonProperty("appliedWriteCount")]
    public int AppliedWriteCount { get; set; }

    /// <summary>
    ///     Gets or sets the stable replica contract identity.
    /// </summary>
    [JsonProperty("contractIdentity")]
    public string ContractIdentity { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the runtime-owned delivery key.
    /// </summary>
    [JsonProperty("deliveryKey")]
    public string DeliveryKey { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the document identifier.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets a value indicating whether the latest write was a delete.
    /// </summary>
    [JsonProperty("isDeleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    ///     Gets or sets the UTC timestamp when the lane was last updated.
    /// </summary>
    [JsonProperty("lastUpdatedAtUtc")]
    public string LastUpdatedAtUtc { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the serialized latest payload, when present.
    /// </summary>
    [JsonProperty("latestPayloadJson")]
    public string? LatestPayloadJson { get; set; }

    /// <summary>
    ///     Gets or sets the latest applied source position for the lane.
    /// </summary>
    [JsonProperty("latestSourcePosition")]
    public long LatestSourcePosition { get; set; }

    /// <summary>
    ///     Gets or sets the partition key for the target.
    /// </summary>
    [JsonProperty("replicaPartitionKey")]
    public string ReplicaPartitionKey { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the provider-facing target name.
    /// </summary>
    [JsonProperty("targetName")]
    public string TargetName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the document discriminator.
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = CosmosReplicaSinkDocumentKeys.TargetDeliveryDocumentType;

    /// <summary>
    ///     Creates a target delivery document from the supplied provider write request.
    /// </summary>
    /// <param name="request">The provider write request.</param>
    /// <param name="appliedWriteCount">The cumulative applied-write count for the lane.</param>
    /// <param name="updatedAtUtc">The UTC timestamp when the lane was updated.</param>
    /// <returns>The target delivery document.</returns>
    public static CosmosReplicaSinkTargetDeliveryDocument Create(
        ReplicaWriteRequest request,
        int appliedWriteCount,
        DateTimeOffset updatedAtUtc
    ) =>
        new()
        {
            AppliedWriteCount = appliedWriteCount,
            ContractIdentity = request.ContractIdentity,
            DeliveryKey = request.DeliveryKey,
            Id = CosmosReplicaSinkDocumentKeys.TargetDeliveryId(request.DeliveryKey),
            IsDeleted = request.IsDeleted,
            LastUpdatedAtUtc = CosmosReplicaSinkDocumentKeys.FormatUtcTimestamp(updatedAtUtc),
            LatestPayloadJson = CosmosReplicaSinkDocumentKeys.SerializePayload(request.Payload),
            LatestSourcePosition = request.SourcePosition,
            ReplicaPartitionKey = request.Target.DestinationIdentity.TargetName,
            TargetName = request.Target.DestinationIdentity.TargetName,
            Type = CosmosReplicaSinkDocumentKeys.TargetDeliveryDocumentType,
        };

    /// <summary>
    ///     Gets the latest payload represented by the document.
    /// </summary>
    /// <returns>The deserialized payload, if present.</returns>
    public object? GetLatestPayload() => CosmosReplicaSinkDocumentKeys.DeserializePayload(LatestPayloadJson);
}