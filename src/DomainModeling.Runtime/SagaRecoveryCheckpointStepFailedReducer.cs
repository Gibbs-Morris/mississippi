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
        Guid? inFlightAttemptId = eventData.AttemptId == Guid.Empty ? null : eventData.AttemptId;
        string? inFlightOperationKey = string.IsNullOrWhiteSpace(eventData.OperationKey)
            ? null
            : eventData.OperationKey;
        return state with
        {
            PendingDirection = SagaExecutionDirection.Forward,
            PendingStepIndex = eventData.StepIndex,
            PendingStepName = eventData.StepName,
            InFlightAttemptId = inFlightAttemptId,
            InFlightOperationKey = inFlightOperationKey,
            BlockedReason = null,
            NextEligibleResumeAt = null,
        };
    }
}