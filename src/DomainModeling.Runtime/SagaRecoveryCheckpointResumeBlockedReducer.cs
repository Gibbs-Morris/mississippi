using System;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Rebuilds checkpoint state from <see cref="SagaResumeBlocked" />.
/// </summary>
internal sealed class SagaRecoveryCheckpointResumeBlockedReducer
    : EventReducerBase<SagaResumeBlocked, SagaRecoveryCheckpoint>
{
    /// <inheritdoc />
    protected override SagaRecoveryCheckpoint ReduceCore(
        SagaRecoveryCheckpoint state,
        SagaResumeBlocked eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        bool isAutomaticResume = eventData.Source is SagaResumeSource.Reminder;
        return state with
        {
            PendingDirection = eventData.Direction,
            PendingStepIndex = eventData.StepIndex,
            PendingStepName = eventData.StepName,
            InFlightAttemptId = null,
            InFlightOperationKey = null,
            BlockedReason = eventData.BlockedReason,
            LastResumeSource = eventData.Source,
            LastResumeAttemptedAt = eventData.BlockedAt,
            AutomaticAttemptCount = isAutomaticResume ? state.AutomaticAttemptCount + 1 : state.AutomaticAttemptCount,
            NextEligibleResumeAt = null,
            ReminderArmed = false,
        };
    }
}