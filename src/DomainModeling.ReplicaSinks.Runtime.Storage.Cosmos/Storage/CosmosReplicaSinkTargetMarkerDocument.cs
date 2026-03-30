using System;

using Newtonsoft.Json;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.Storage;

/// <summary>
///     Cosmos document that marks a provisioned replica target.
/// </summary>
internal sealed class CosmosReplicaSinkTargetMarkerDocument
{
    /// <summary>
    ///     Gets or sets the UTC timestamp when the target was provisioned.
    /// </summary>
    [JsonProperty("createdAtUtc")]
    public string CreatedAtUtc { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the document identifier.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = CosmosReplicaSinkDocumentKeys.TargetMarkerId;

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
    public string Type { get; set; } = CosmosReplicaSinkDocumentKeys.TargetMarkerDocumentType;

    /// <summary>
    ///     Creates a target marker document for the specified target.
    /// </summary>
    /// <param name="targetName">The target name.</param>
    /// <param name="createdAtUtc">The UTC timestamp when the target was provisioned.</param>
    /// <returns>The target marker document.</returns>
    public static CosmosReplicaSinkTargetMarkerDocument Create(
        string targetName,
        DateTimeOffset createdAtUtc
    ) =>
        new()
        {
            CreatedAtUtc = CosmosReplicaSinkDocumentKeys.FormatUtcTimestamp(createdAtUtc),
            Id = CosmosReplicaSinkDocumentKeys.TargetMarkerId,
            ReplicaPartitionKey = targetName,
            TargetName = targetName,
            Type = CosmosReplicaSinkDocumentKeys.TargetMarkerDocumentType,
        };
}