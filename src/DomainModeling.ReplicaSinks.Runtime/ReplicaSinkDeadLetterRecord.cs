using System;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Represents a single dead-letter record exposed by the runtime operator surface.
/// </summary>
public sealed class ReplicaSinkDeadLetterRecord
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkDeadLetterRecord" /> class.
    /// </summary>
    public ReplicaSinkDeadLetterRecord(
        string deliveryKey,
        string projectionTypeName,
        string sinkKey,
        string targetName,
        string entityId,
        long sourcePosition,
        int attemptCount,
        string failureCode,
        string? failureSummary,
        bool isFailureSummaryRedacted,
        DateTimeOffset recordedAtUtc,
        long? desiredSourcePosition,
        long? committedSourcePosition
    )
    {
        ArgumentNullException.ThrowIfNull(deliveryKey);
        ArgumentNullException.ThrowIfNull(projectionTypeName);
        ArgumentNullException.ThrowIfNull(sinkKey);
        ArgumentNullException.ThrowIfNull(targetName);
        ArgumentNullException.ThrowIfNull(entityId);
        ArgumentNullException.ThrowIfNull(failureCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(deliveryKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionTypeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sinkKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetName);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(failureCode);
        DeliveryKey = deliveryKey;
        ProjectionTypeName = projectionTypeName;
        SinkKey = sinkKey;
        TargetName = targetName;
        EntityId = entityId;
        SourcePosition = sourcePosition;
        AttemptCount = attemptCount;
        FailureCode = failureCode;
        FailureSummary = failureSummary;
        IsFailureSummaryRedacted = isFailureSummaryRedacted;
        RecordedAtUtc = recordedAtUtc;
        DesiredSourcePosition = desiredSourcePosition;
        CommittedSourcePosition = committedSourcePosition;
    }

    /// <summary>
    ///     Gets the retry/dead-letter attempt count.
    /// </summary>
    public int AttemptCount { get; }

    /// <summary>
    ///     Gets the latest committed source position for the lane, when present.
    /// </summary>
    public long? CommittedSourcePosition { get; }

    /// <summary>
    ///     Gets the runtime-owned delivery key.
    /// </summary>
    public string DeliveryKey { get; }

    /// <summary>
    ///     Gets the latest desired source position for the lane, when present.
    /// </summary>
    public long? DesiredSourcePosition { get; }

    /// <summary>
    ///     Gets the replicated entity identifier.
    /// </summary>
    public string EntityId { get; }

    /// <summary>
    ///     Gets the stable sanitized failure code.
    /// </summary>
    public string FailureCode { get; }

    /// <summary>
    ///     Gets the sanitized failure summary, when exposed to the caller.
    /// </summary>
    public string? FailureSummary { get; }

    /// <summary>
    ///     Gets a value indicating whether the failure summary was redacted for the current caller.
    /// </summary>
    public bool IsFailureSummaryRedacted { get; }

    /// <summary>
    ///     Gets the projection type name.
    /// </summary>
    public string ProjectionTypeName { get; }

    /// <summary>
    ///     Gets the UTC timestamp when the dead-letter entry was recorded.
    /// </summary>
    public DateTimeOffset RecordedAtUtc { get; }

    /// <summary>
    ///     Gets the named sink registration key.
    /// </summary>
    public string SinkKey { get; }

    /// <summary>
    ///     Gets the dead-letter source position.
    /// </summary>
    public long SourcePosition { get; }

    /// <summary>
    ///     Gets the provider-neutral target name.
    /// </summary>
    public string TargetName { get; }
}
