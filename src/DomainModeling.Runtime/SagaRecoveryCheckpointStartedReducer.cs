using System;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Rebuilds checkpoint state from <see cref="SagaStartedEvent" />.
/// </summary>
internal sealed class SagaRecoveryCheckpointStartedReducer : EventReducerBase<SagaStartedEvent, SagaRecoveryCheckpoint>
{
    /// <inheritdoc />
    protected override SagaRecoveryCheckpoint ReduceCore(
        SagaRecoveryCheckpoint state,
        SagaStartedEvent eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return state with
        {
            AccessContextFingerprint = eventData.AccessContextFingerprint,
            SagaId = eventData.SagaId,
            StepHash = eventData.StepHash,
            RecoveryMode = eventData.RecoveryMode,
            RecoveryProfile = eventData.RecoveryProfile,
            PendingDirection = SagaExecutionDirection.Forward,
            PendingStepIndex = 0,
            PendingStepName = null,
            InFlightAttemptId = null,
            InFlightOperationKey = null,
            BlockedReason = null,
            LastResumeSource = null,
            LastResumeAttemptedAt = null,
            AutomaticAttemptCount = 0,
            NextEligibleResumeAt = null,
            ReminderArmed = false,
        };
    }
}