using System;


namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Describes the operator-facing runtime recovery status for a saga without exposing raw domain state.
/// </summary>
public sealed record SagaRuntimeStatus
{
    /// <summary>
    ///     Gets the number of automatic reminder-driven attempts observed for the saga.
    /// </summary>
    public int AutomaticAttemptCount { get; init; }

    /// <summary>
    ///     Gets the operator-visible blocked reason, when manual intervention is currently required.
    /// </summary>
    public string? BlockedReason { get; init; }

    /// <summary>
    ///     Gets the timestamp of the most recent explicit resume attempt.
    /// </summary>
    public DateTimeOffset? LastResumeAttemptedAt { get; init; }

    /// <summary>
    ///     Gets the source of the most recent explicit resume attempt.
    /// </summary>
    public SagaResumeSource? LastResumeSource { get; init; }

    /// <summary>
    ///     Gets the next eligible resume timestamp when backoff has been computed.
    /// </summary>
    public DateTimeOffset? NextEligibleResumeAt { get; init; }

    /// <summary>
    ///     Gets the pending execution direction, when the saga has resumable work.
    /// </summary>
    public SagaExecutionDirection? PendingDirection { get; init; }

    /// <summary>
    ///     Gets the pending step index, when the saga has resumable work.
    /// </summary>
    public int? PendingStepIndex { get; init; }

    /// <summary>
    ///     Gets the pending step name, when the saga has resumable work.
    /// </summary>
    public string? PendingStepName { get; init; }

    /// <summary>
    ///     Gets the current saga phase.
    /// </summary>
    public required string Phase { get; init; }

    /// <summary>
    ///     Gets the configured saga-level recovery mode.
    /// </summary>
    public required SagaRecoveryMode RecoveryMode { get; init; }

    /// <summary>
    ///     Gets the currently visible runtime recovery disposition.
    /// </summary>
    public required SagaResumeDisposition ResumeDisposition { get; init; }

    /// <summary>
    ///     Gets a value indicating whether a reminder is currently armed for this saga.
    /// </summary>
    public bool ReminderArmed { get; init; }

    /// <summary>
    ///     Gets the saga identifier.
    /// </summary>
    public required Guid SagaId { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the persisted workflow hash matches the currently generated saga definition.
    /// </summary>
    public bool WorkflowHashMatches { get; init; }
}

/// <summary>
///     Describes the operator-visible recovery disposition for a saga.
/// </summary>
public enum SagaResumeDisposition
{
    /// <summary>
    ///     The saga is idle and does not currently require a recovery action.
    /// </summary>
    Idle,

    /// <summary>
    ///     The saga has pending work that can resume automatically.
    /// </summary>
    AutomaticPending,

    /// <summary>
    ///     The saga requires explicit manual intervention before work can continue.
    /// </summary>
    ManualInterventionRequired,

    /// <summary>
    ///     The saga is in a terminal phase and no further recovery work will execute.
    /// </summary>
    Terminal,

    /// <summary>
    ///     The saga's persisted workflow identity no longer matches the current generated definition.
    /// </summary>
    WorkflowMismatch,
}