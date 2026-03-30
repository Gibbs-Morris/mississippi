using System;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

/// <summary>
///     Represents a sanitized persisted failure for a single source position.
/// </summary>
public sealed class ReplicaSinkStoredFailure
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkStoredFailure" /> class.
    /// </summary>
    /// <param name="sourcePosition">The source position associated with the failure.</param>
    /// <param name="attemptCount">The cumulative attempt count for the failed source position.</param>
    /// <param name="failureCode">A stable sanitized failure code.</param>
    /// <param name="failureSummary">A sanitized failure summary safe to persist.</param>
    /// <param name="recordedAtUtc">The UTC timestamp when the failure state was recorded.</param>
    /// <param name="nextRetryAtUtc">The next retry timestamp, if this failure represents retry state.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="failureCode" /> or <paramref name="failureSummary" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="failureCode" /> or <paramref name="failureSummary" /> is empty or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="sourcePosition" /> is negative or <paramref name="attemptCount" /> is less than 1.
    /// </exception>
    public ReplicaSinkStoredFailure(
        long sourcePosition,
        int attemptCount,
        string failureCode,
        string failureSummary,
        DateTimeOffset recordedAtUtc,
        DateTimeOffset? nextRetryAtUtc = null
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sourcePosition);
        ArgumentOutOfRangeException.ThrowIfLessThan(attemptCount, 1);
        ArgumentNullException.ThrowIfNull(failureCode);
        ArgumentNullException.ThrowIfNull(failureSummary);
        ArgumentException.ThrowIfNullOrWhiteSpace(failureCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(failureSummary);
        AttemptCount = attemptCount;
        FailureCode = failureCode;
        FailureSummary = failureSummary;
        NextRetryAtUtc = nextRetryAtUtc;
        RecordedAtUtc = recordedAtUtc;
        SourcePosition = sourcePosition;
    }

    /// <summary>
    ///     Gets the cumulative attempt count for the failed source position.
    /// </summary>
    public int AttemptCount { get; }

    /// <summary>
    ///     Gets a stable sanitized failure code.
    /// </summary>
    public string FailureCode { get; }

    /// <summary>
    ///     Gets a sanitized failure summary safe to persist.
    /// </summary>
    public string FailureSummary { get; }

    /// <summary>
    ///     Gets the next retry timestamp, if this failure represents retry state.
    /// </summary>
    public DateTimeOffset? NextRetryAtUtc { get; }

    /// <summary>
    ///     Gets the UTC timestamp when the failure state was recorded.
    /// </summary>
    public DateTimeOffset RecordedAtUtc { get; }

    /// <summary>
    ///     Gets the source position associated with the failure.
    /// </summary>
    public long SourcePosition { get; }
}