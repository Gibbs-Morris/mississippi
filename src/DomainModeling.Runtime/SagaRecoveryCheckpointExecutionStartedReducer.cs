using System;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Rebuilds checkpoint state from <see cref="SagaStepExecutionStarted" />.
/// </summary>
internal sealed class SagaRecoveryCheckpointExecutionStartedReducer
    : EventReducerBase<SagaStepExecutionStarted, SagaRecoveryCheckpoint>
{
    /// <inheritdoc />
    protected override SagaRecoveryCheckpoint ReduceCore(
        SagaRecoveryCheckpoint state,
        SagaStepExecutionStarted eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        bool isExplicitResume = eventData.Source is not SagaResumeSource.Initial;
        bool isAutomaticResume = eventData.Source is SagaResumeSource.Reminder;
        return state with
        {
            PendingDirection = eventData.Direction,
            PendingStepIndex = eventData.StepIndex,
            PendingStepName = eventData.StepName,
            InFlightAttemptId = eventData.AttemptId,
            InFlightOperationKey = eventData.OperationKey,
            BlockedReason = null,
            AccessContextFingerprint = eventData.Source is SagaResumeSource.Manual
                ? eventData.AccessContextFingerprint ?? state.AccessContextFingerprint
                : state.AccessContextFingerprint,
            LastResumeSource = isExplicitResume ? eventData.Source : state.LastResumeSource,
            LastResumeAttemptedAt = isExplicitResume ? eventData.StartedAt : state.LastResumeAttemptedAt,
            AutomaticAttemptCount = isAutomaticResume ? state.AutomaticAttemptCount + 1 : state.AutomaticAttemptCount,
            NextEligibleResumeAt = null,
        };
    }
}