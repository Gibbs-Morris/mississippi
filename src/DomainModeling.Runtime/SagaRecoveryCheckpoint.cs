using System;

using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.DomainModeling.Abstractions;

using Orleans;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Framework-owned recovery checkpoint rebuilt from persisted saga infrastructure events.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.DomainModeling.Runtime.SagaRecoveryCheckpoint")]
[SnapshotStorageName("MISSISSIPPI", "SAGAS", "SAGARECOVERYCHECKPOINT")]
internal sealed record SagaRecoveryCheckpoint
{
    /// <summary>
    ///     Gets the optional access-context fingerprint associated with the latest authorized resume attempt.
    /// </summary>
    [Id(15)]
    public string? AccessContextFingerprint { get; init; }

    /// <summary>
    ///     Gets the number of automatic reminder-driven attempts observed in the checkpoint.
    /// </summary>
    [Id(12)]
    public int AutomaticAttemptCount { get; init; }

    /// <summary>
    ///     Gets the operator-visible blocked reason, when one has been persisted.
    /// </summary>
    [Id(9)]
    public string? BlockedReason { get; init; }

    /// <summary>
    ///     Gets the in-flight attempt identifier for the currently pending step execution.
    /// </summary>
    [Id(7)]
    public Guid? InFlightAttemptId { get; init; }

    /// <summary>
    ///     Gets the in-flight framework operation key for the currently pending step execution.
    /// </summary>
    [Id(8)]
    public string? InFlightOperationKey { get; init; }

    /// <summary>
    ///     Gets the timestamp of the most recent explicit resume attempt.
    /// </summary>
    [Id(11)]
    public DateTimeOffset? LastResumeAttemptedAt { get; init; }

    /// <summary>
    ///     Gets the source of the most recent explicit resume attempt.
    /// </summary>
    [Id(10)]
    public SagaResumeSource? LastResumeSource { get; init; }

    /// <summary>
    ///     Gets the next eligible resume timestamp when backoff has been computed.
    /// </summary>
    [Id(13)]
    public DateTimeOffset? NextEligibleResumeAt { get; init; }

    /// <summary>
    ///     Gets the pending execution direction, when the saga has resumable work.
    /// </summary>
    [Id(4)]
    public SagaExecutionDirection? PendingDirection { get; init; }

    /// <summary>
    ///     Gets the pending step index, when the saga has resumable work.
    /// </summary>
    [Id(5)]
    public int? PendingStepIndex { get; init; }

    /// <summary>
    ///     Gets the pending step name, when known.
    /// </summary>
    [Id(6)]
    public string? PendingStepName { get; init; }

    /// <summary>
    ///     Gets the configured saga-level recovery mode.
    /// </summary>
    [Id(2)]
    public SagaRecoveryMode RecoveryMode { get; init; }

    /// <summary>
    ///     Gets the configured saga-level recovery profile.
    /// </summary>
    [Id(3)]
    public string? RecoveryProfile { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the reminder is currently armed for this saga.
    /// </summary>
    [Id(14)]
    public bool ReminderArmed { get; init; }

    /// <summary>
    ///     Gets the saga identifier associated with this checkpoint.
    /// </summary>
    [Id(0)]
    public Guid SagaId { get; init; }

    /// <summary>
    ///     Gets the workflow step hash captured when the saga started.
    /// </summary>
    [Id(1)]
    public string StepHash { get; init; } = string.Empty;
}