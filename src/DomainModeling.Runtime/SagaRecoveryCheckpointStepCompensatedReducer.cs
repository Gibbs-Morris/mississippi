using System;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Rebuilds checkpoint state from <see cref="SagaStepCompensated" />.
/// </summary>
internal sealed class SagaRecoveryCheckpointStepCompensatedReducer
    : EventReducerBase<SagaStepCompensated, SagaRecoveryCheckpoint>
{
    /// <inheritdoc />
    protected override SagaRecoveryCheckpoint ReduceCore(
        SagaRecoveryCheckpoint state,
        SagaStepCompensated eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return SagaRecoveryCheckpointReducerState.ClearInFlight(state) with
        {
            PendingDirection = SagaExecutionDirection.Compensation,
            PendingStepIndex = eventData.StepIndex - 1,
            BlockedReason = null,
        };
    }
}