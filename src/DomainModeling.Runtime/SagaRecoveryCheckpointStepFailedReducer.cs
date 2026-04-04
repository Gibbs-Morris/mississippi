using System;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Rebuilds checkpoint state from <see cref="SagaStepFailed" />.
/// </summary>
internal sealed class SagaRecoveryCheckpointStepFailedReducer : EventReducerBase<SagaStepFailed, SagaRecoveryCheckpoint>
{
    /// <inheritdoc />
    protected override SagaRecoveryCheckpoint ReduceCore(
        SagaRecoveryCheckpoint state,
        SagaStepFailed eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return state with
        {
            PendingDirection = SagaExecutionDirection.Forward,
            PendingStepIndex = eventData.StepIndex,
            PendingStepName = eventData.StepName,
            InFlightAttemptId = eventData.AttemptId,
            InFlightOperationKey = eventData.OperationKey,
            BlockedReason = null,
            NextEligibleResumeAt = null,
        };
    }
}