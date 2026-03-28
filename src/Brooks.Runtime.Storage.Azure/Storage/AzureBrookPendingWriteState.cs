using System;
using System.Text.Json.Serialization;

using Azure;


namespace Mississippi.Brooks.Runtime.Storage.Azure.Storage;

/// <summary>
///     Represents persisted pending-write state for a Brooks Azure append attempt.
/// </summary>
internal sealed record AzureBrookPendingWriteState
{
    /// <summary>
    ///     Gets the append-attempt identifier.
    /// </summary>
    public string AttemptId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the UTC instant when the pending write was created.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; init; }

    /// <summary>
    ///     Gets the blob ETag used for conditional cleanup.
    /// </summary>
    [JsonIgnore]
    public ETag ETag { get; init; }

    /// <summary>
    ///     Gets the number of events in the pending batch.
    /// </summary>
    public int EventCount { get; init; }

    /// <summary>
    ///     Gets the lease identifier that fenced the writer.
    /// </summary>
    public string LeaseId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the committed cursor position before the append began.
    /// </summary>
    public long OriginalPosition { get; init; }

    /// <summary>
    ///     Gets the persisted document schema version.
    /// </summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>
    ///     Gets the cursor position that will be committed if the batch succeeds.
    /// </summary>
    public long TargetPosition { get; init; }

    /// <summary>
    ///     Gets the monotonic write epoch for the pending append.
    /// </summary>
    public long WriteEpoch { get; init; }
}