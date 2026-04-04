using System;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Rebuilds checkpoint state from <see cref="SagaStepCompleted" />.
/// </summary>
internal sealed class SagaRecoveryCheckpointStepCompletedReducer
    : EventReducerBase<SagaStepCompleted, SagaRecoveryCheckpoint>
{
    /// <inheritdoc />
    protected override SagaRecoveryCheckpoint ReduceCore(
        SagaRecoveryCheckpoint state,
        SagaStepCompleted eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return SagaRecoveryCheckpointReducerState.ClearInFlight(state) with
        {
            PendingDirection = SagaExecutionDirection.Forward,
            PendingStepIndex = eventData.StepIndex + 1,
            BlockedReason = null,
        };
    }
}