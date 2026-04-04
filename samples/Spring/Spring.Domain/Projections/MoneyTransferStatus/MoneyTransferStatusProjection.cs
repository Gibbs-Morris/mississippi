using System;

using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace MississippiSamples.Spring.Domain.Projections.MoneyTransferStatus;

/// <summary>
///     Read-optimized projection for tracking money transfer saga status.
/// </summary>
[ProjectionPath("money-transfer-status")]
[BrookName("SPRING", "BANKING", "TRANSFER")]
[SnapshotStorageName("SPRING", "BANKING", "TRANSFERSTATUS")]
[GenerateProjectionEndpoints]
[GenerateMcpReadTool(
    Title = "Get Money Transfer Status",
    Description = "Retrieves the current status and phase of a money transfer saga.")]
[GenerateSerializer]
[GenerateSagaStatusReducers]
[Alias("MississippiSamples.Spring.Domain.Projections.MoneyTransferStatus.MoneyTransferStatusProjection")]
public sealed record MoneyTransferStatusProjection
{
    /// <summary>
    ///     Gets the number of automatic reminder-driven attempts observed for the saga.
    /// </summary>
    [Id(14)]
    public int AutomaticAttemptCount { get; init; }

    /// <summary>
    ///     Gets the operator-visible blocked reason when manual intervention is required.
    /// </summary>
    [Id(11)]
    public string? BlockedReason { get; init; }

    /// <summary>
    ///     Gets the timestamp when the saga completed or failed.
    /// </summary>
    [Id(5)]
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    ///     Gets the error code for the last failure, if any.
    /// </summary>
    [Id(2)]
    public string? ErrorCode { get; init; }

    /// <summary>
    ///     Gets the error message for the last failure, if any.
    /// </summary>
    [Id(3)]
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Gets the index of the last completed step.
    /// </summary>
    [Id(1)]
    public int LastCompletedStepIndex { get; init; } = -1;

    /// <summary>
    ///     Gets the timestamp of the most recent explicit resume attempt.
    /// </summary>
    [Id(13)]
    public DateTimeOffset? LastResumeAttemptedAt { get; init; }

    /// <summary>
    ///     Gets the source of the most recent explicit resume attempt.
    /// </summary>
    [Id(12)]
    public SagaResumeSource? LastResumeSource { get; init; }

    /// <summary>
    ///     Gets the pending execution direction when the saga has resumable work.
    /// </summary>
    [Id(7)]
    public SagaExecutionDirection? PendingDirection { get; init; }

    /// <summary>
    ///     Gets the pending step index when the saga has resumable work.
    /// </summary>
    [Id(8)]
    public int? PendingStepIndex { get; init; }

    /// <summary>
    ///     Gets the pending step name when known.
    /// </summary>
    [Id(9)]
    public string? PendingStepName { get; init; }

    /// <summary>
    ///     Gets the current saga phase.
    /// </summary>
    [Id(0)]
    public SagaPhase Phase { get; init; } = SagaPhase.NotStarted;

    /// <summary>
    ///     Gets the configured saga-level recovery mode.
    /// </summary>
    [Id(6)]
    public SagaRecoveryMode RecoveryMode { get; init; } = SagaRecoveryMode.Automatic;

    /// <summary>
    ///     Gets the operator-visible recovery disposition.
    /// </summary>
    [Id(10)]
    public SagaResumeDisposition ResumeDisposition { get; init; } = SagaResumeDisposition.Idle;

    /// <summary>
    ///     Gets the timestamp when the saga started.
    /// </summary>
    [Id(4)]
    public DateTimeOffset? StartedAt { get; init; }
}