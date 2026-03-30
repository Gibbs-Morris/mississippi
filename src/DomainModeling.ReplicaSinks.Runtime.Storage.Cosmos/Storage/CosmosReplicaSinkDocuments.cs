#if false
using System;
using System.Globalization;

using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

using Newtonsoft.Json;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.Storage;

/// <summary>
///     Defines internal document identifiers and serialization helpers for Cosmos-backed replica sinks.
/// </summary>
internal static class CosmosReplicaSinkDocumentKeys
{
    internal const string DeliveryStateDocumentType = "state";

    internal const string TargetDeliveryDocumentType = "delivery";

    internal const string TargetMarkerDocumentType = "target";

    internal const string TargetMarkerId = "target";

    internal static string DeliveryStateId(
        string deliveryKey
    ) => $"state::{deliveryKey}";

    internal static string FormatUtcTimestamp(
        DateTimeOffset timestamp
    ) => timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    internal static DateTimeOffset? ParseNullableUtcTimestamp(
        string? value
    ) => string.IsNullOrWhiteSpace(value)
        ? null
        : DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    internal static object? DeserializePayload(
        string? payloadJson
    ) => payloadJson is null ? null : JsonConvert.DeserializeObject<object>(payloadJson);

    internal static string? SerializePayload(
        object? payload
    ) => payload is null ? null : JsonConvert.SerializeObject(payload);

    internal static string TargetDeliveryId(
        string deliveryKey
    ) => $"delivery::{deliveryKey}";
}

/// <summary>
///     Represents the aggregate inspection summary produced from Cosmos target documents.
/// </summary>
internal sealed class CosmosReplicaSinkTargetInspectionSnapshot
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CosmosReplicaSinkTargetInspectionSnapshot" /> class.
    /// </summary>
    /// <param name="targetExists">A value indicating whether the target marker exists.</param>
    /// <param name="writeCount">The total number of applied writes observed for the target.</param>
    /// <param name="latestSourcePosition">The latest applied source position, when present.</param>
    /// <param name="latestPayload">The latest applied payload, when present.</param>
    public CosmosReplicaSinkTargetInspectionSnapshot(
        bool targetExists,
        long writeCount,
        long? latestSourcePosition = null,
        object? latestPayload = null
    )
    {
        TargetExists = targetExists;
        WriteCount = writeCount;
        LatestSourcePosition = latestSourcePosition;
        LatestPayload = latestPayload;
    }

    /// <summary>
    ///     Gets the latest applied payload.
    /// </summary>
    public object? LatestPayload { get; }

    /// <summary>
    ///     Gets the latest applied source position.
    /// </summary>
    public long? LatestSourcePosition { get; }

    /// <summary>
    ///     Gets a value indicating whether the target marker exists.
    /// </summary>
    public bool TargetExists { get; }

    /// <summary>
    ///     Gets the total number of applied writes observed for the target.
    /// </summary>
    public long WriteCount { get; }
}

/// <summary>
///     Cosmos document that marks a provisioned replica target.
/// </summary>
internal sealed class CosmosReplicaSinkTargetMarkerDocument
{
    /// <summary>
    ///     Creates a target marker document for the specified target.
    /// </summary>
    /// <param name="targetName">The target name.</param>
    /// <param name="createdAtUtc">The UTC timestamp when the target was provisioned.</param>
    /// <returns>The target marker document.</returns>
    public static CosmosReplicaSinkTargetMarkerDocument Create(
        string targetName,
        DateTimeOffset createdAtUtc
    ) => new()
    {
        CreatedAtUtc = CosmosReplicaSinkDocumentKeys.FormatUtcTimestamp(createdAtUtc),
        Id = CosmosReplicaSinkDocumentKeys.TargetMarkerId,
        ReplicaPartitionKey = targetName,
        TargetName = targetName,
        Type = CosmosReplicaSinkDocumentKeys.TargetMarkerDocumentType,
    };

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
}

/// <summary>
///     Cosmos document that stores the latest-state projection for a delivery lane.
/// </summary>
internal sealed class CosmosReplicaSinkTargetDeliveryDocument
{
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
    ) => new()
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
}

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

/// <summary>
///     Cosmos representation of a stored retry or dead-letter failure.
/// </summary>
internal sealed class CosmosReplicaSinkStoredFailureDocument
{
    /// <summary>
    ///     Creates a Cosmos document from the supplied stored failure.
    /// </summary>
    /// <param name="failure">The stored failure.</param>
    /// <returns>The Cosmos representation, or <see langword="null" /> when no failure exists.</returns>
    public static CosmosReplicaSinkStoredFailureDocument? FromDomain(
        ReplicaSinkStoredFailure? failure
    ) => failure is null
        ? null
        : new CosmosReplicaSinkStoredFailureDocument
        {
            AttemptCount = failure.AttemptCount,
            FailureCode = failure.FailureCode,
            FailureSummary = failure.FailureSummary,
            NextRetryAtUtc = failure.NextRetryAtUtc is null
                ? null
                : CosmosReplicaSinkDocumentKeys.FormatUtcTimestamp(failure.NextRetryAtUtc.Value),
            RecordedAtUtc = CosmosReplicaSinkDocumentKeys.FormatUtcTimestamp(failure.RecordedAtUtc),
            SourcePosition = failure.SourcePosition,
        };

    /// <summary>
    ///     Converts the Cosmos document into the stored failure abstraction.
    /// </summary>
    /// <returns>The stored failure abstraction.</returns>
    public ReplicaSinkStoredFailure ToDomain() => new(
        SourcePosition,
        AttemptCount,
        FailureCode,
        FailureSummary,
        CosmosReplicaSinkDocumentKeys.ParseNullableUtcTimestamp(RecordedAtUtc)!.Value,
        CosmosReplicaSinkDocumentKeys.ParseNullableUtcTimestamp(NextRetryAtUtc));

    /// <summary>
    ///     Gets or sets the cumulative attempt count.
    /// </summary>
    [JsonProperty("attemptCount")]
    public int AttemptCount { get; set; }

    /// <summary>
    ///     Gets or sets the stable sanitized failure code.
    /// </summary>
    [JsonProperty("failureCode")]
    public string FailureCode { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the sanitized failure summary safe to persist.
    /// </summary>
    [JsonProperty("failureSummary")]
    public string FailureSummary { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the next retry timestamp, when the failure represents retry state.
    /// </summary>
    [JsonProperty("nextRetryAtUtc")]
    public string? NextRetryAtUtc { get; set; }

    /// <summary>
    ///     Gets or sets the UTC timestamp when the failure was recorded.
    /// </summary>
    [JsonProperty("recordedAtUtc")]
    public string RecordedAtUtc { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the source position associated with the failure.
    /// </summary>
    [JsonProperty("sourcePosition")]
    public long SourcePosition { get; set; }
}
#endif
