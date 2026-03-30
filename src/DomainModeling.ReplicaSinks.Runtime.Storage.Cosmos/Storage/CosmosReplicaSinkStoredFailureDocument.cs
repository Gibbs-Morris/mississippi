using System;

using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

using Newtonsoft.Json;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.Storage;

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
    )
    {
        if (failure is null)
        {
            return null;
        }

        return new CosmosReplicaSinkStoredFailureDocument
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
    }

    /// <summary>
    ///     Converts the Cosmos document into the stored failure abstraction.
    /// </summary>
    /// <returns>The stored failure abstraction.</returns>
    public ReplicaSinkStoredFailure ToDomain()
    {
        DateTimeOffset recordedAtUtc = CosmosReplicaSinkDocumentKeys.ParseNullableUtcTimestamp(RecordedAtUtc) ?? default;
        DateTimeOffset? nextRetryAtUtc = CosmosReplicaSinkDocumentKeys.ParseNullableUtcTimestamp(NextRetryAtUtc);
        return new(
            SourcePosition,
            AttemptCount,
            FailureCode,
            FailureSummary,
            recordedAtUtc,
            nextRetryAtUtc);
    }

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
