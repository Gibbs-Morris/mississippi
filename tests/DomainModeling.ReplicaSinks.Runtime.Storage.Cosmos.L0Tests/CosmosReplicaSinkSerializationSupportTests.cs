using System;
using System.Collections.Generic;

using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.Storage;

using Newtonsoft.Json.Linq;


namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.L0Tests;

/// <summary>
///     Tests small Cosmos serialization helpers that sit underneath the provider and delivery-state store flows.
/// </summary>
public sealed class CosmosReplicaSinkSerializationSupportTests
{
    /// <summary>
    ///     Ensures document identifiers and UTC timestamp formatting stay stable for persisted documents.
    /// </summary>
    [Fact]
    public void CosmosReplicaSinkDocumentKeysShouldCreateStableIdentifiersAndUtcTimestamps()
    {
        DateTimeOffset timestamp = new(2026, 3, 30, 8, 15, 0, TimeSpan.FromHours(-4));
        string deliveryStateId = CosmosReplicaSinkDocumentKeys.DeliveryStateId("Projection::sink-a::orders::entity-1");
        string targetDeliveryId =
            CosmosReplicaSinkDocumentKeys.TargetDeliveryId("Projection::sink-a::orders::entity-1");
        string formattedTimestamp = CosmosReplicaSinkDocumentKeys.FormatUtcTimestamp(timestamp);
        DateTimeOffset? parsedTimestamp = CosmosReplicaSinkDocumentKeys.ParseNullableUtcTimestamp(formattedTimestamp);
        Assert.Equal("state::Projection::sink-a::orders::entity-1", deliveryStateId);
        Assert.Equal("delivery::Projection::sink-a::orders::entity-1", targetDeliveryId);
        Assert.EndsWith("+00:00", formattedTimestamp, StringComparison.Ordinal);
        Assert.Equal(timestamp.ToUniversalTime(), parsedTimestamp);
    }

    /// <summary>
    ///     Ensures missing payloads and timestamps remain null instead of fabricating stored values.
    /// </summary>
    [Fact]
    public void CosmosReplicaSinkDocumentKeysShouldPreserveMissingPayloadAndTimestampValues()
    {
        Assert.Null(CosmosReplicaSinkDocumentKeys.SerializePayload(null));
        Assert.Null(CosmosReplicaSinkDocumentKeys.DeserializePayload(null));
        Assert.Null(CosmosReplicaSinkDocumentKeys.ParseNullableUtcTimestamp(null));
        Assert.Null(CosmosReplicaSinkDocumentKeys.ParseNullableUtcTimestamp("   "));
    }

    /// <summary>
    ///     Ensures payload snapshots serialize and deserialize without losing their JSON shape.
    /// </summary>
    [Fact]
    public void CosmosReplicaSinkDocumentKeysShouldRoundTripStructuredPayloads()
    {
        Dictionary<string, object?> payload = new(StringComparer.Ordinal)
        {
            ["id"] = "order-1",
            ["quantity"] = 2,
        };
        string? serialized = CosmosReplicaSinkDocumentKeys.SerializePayload(payload);
        JObject roundTripped = Assert.IsType<JObject>(CosmosReplicaSinkDocumentKeys.DeserializePayload(serialized));
        Assert.NotNull(serialized);
        Assert.Equal("order-1", roundTripped["id"]!.Value<string>());
        Assert.Equal(2, roundTripped["quantity"]!.Value<int>());
    }

    /// <summary>
    ///     Ensures absent failures do not produce persisted failure documents.
    /// </summary>
    [Fact]
    public void CosmosReplicaSinkStoredFailureDocumentFromDomainShouldReturnNullForMissingFailure()
    {
        Assert.Null(CosmosReplicaSinkStoredFailureDocument.FromDomain(null));
    }

    /// <summary>
    ///     Ensures missing recorded timestamps default conservatively instead of crashing deserialization.
    /// </summary>
    [Fact]
    public void CosmosReplicaSinkStoredFailureDocumentShouldDefaultMissingRecordedTimestamp()
    {
        CosmosReplicaSinkStoredFailureDocument document = new()
        {
            AttemptCount = 2,
            FailureCode = "dead_letter",
            FailureSummary = "Persisted safe summary.",
            RecordedAtUtc = " ",
            SourcePosition = 11,
        };
        ReplicaSinkStoredFailure failure = document.ToDomain();
        Assert.Equal(11, failure.SourcePosition);
        Assert.Equal(2, failure.AttemptCount);
        Assert.Equal("dead_letter", failure.FailureCode);
        Assert.Equal("Persisted safe summary.", failure.FailureSummary);
        Assert.Equal(default, failure.RecordedAtUtc);
        Assert.Null(failure.NextRetryAtUtc);
    }

    /// <summary>
    ///     Ensures dead-letter failures without retry timestamps stay that way after round-tripping.
    /// </summary>
    [Fact]
    public void CosmosReplicaSinkStoredFailureDocumentShouldRoundTripDeadLetterFailuresWithoutRetryTimestamps()
    {
        ReplicaSinkStoredFailure failure = new(
            99,
            1,
            "dead_letter",
            "Permanent failure.",
            new(2026, 3, 30, 12, 10, 0, TimeSpan.Zero));
        CosmosReplicaSinkStoredFailureDocument document = Assert.IsType<CosmosReplicaSinkStoredFailureDocument>(
            CosmosReplicaSinkStoredFailureDocument.FromDomain(failure));
        ReplicaSinkStoredFailure roundTripped = document.ToDomain();
        Assert.Null(document.NextRetryAtUtc);
        Assert.Null(roundTripped.NextRetryAtUtc);
        Assert.Equal(failure.RecordedAtUtc, roundTripped.RecordedAtUtc);
        Assert.Equal(failure.FailureSummary, roundTripped.FailureSummary);
    }

    /// <summary>
    ///     Ensures retry-state failures round-trip through the Cosmos document representation.
    /// </summary>
    [Fact]
    public void CosmosReplicaSinkStoredFailureDocumentShouldRoundTripRetryFailures()
    {
        ReplicaSinkStoredFailure failure = new(
            42,
            3,
            "retry_failure",
            "Retry me later.",
            new(2026, 3, 30, 12, 0, 0, TimeSpan.Zero),
            new(2026, 3, 30, 12, 5, 0, TimeSpan.Zero));
        CosmosReplicaSinkStoredFailureDocument document = Assert.IsType<CosmosReplicaSinkStoredFailureDocument>(
            CosmosReplicaSinkStoredFailureDocument.FromDomain(failure));
        ReplicaSinkStoredFailure roundTripped = document.ToDomain();
        Assert.Equal(3, document.AttemptCount);
        Assert.Equal("retry_failure", document.FailureCode);
        Assert.Equal("Retry me later.", document.FailureSummary);
        Assert.NotNull(document.NextRetryAtUtc);
        Assert.Equal(failure.SourcePosition, roundTripped.SourcePosition);
        Assert.Equal(failure.AttemptCount, roundTripped.AttemptCount);
        Assert.Equal(failure.FailureCode, roundTripped.FailureCode);
        Assert.Equal(failure.FailureSummary, roundTripped.FailureSummary);
        Assert.Equal(failure.RecordedAtUtc, roundTripped.RecordedAtUtc);
        Assert.Equal(failure.NextRetryAtUtc, roundTripped.NextRetryAtUtc);
    }
}