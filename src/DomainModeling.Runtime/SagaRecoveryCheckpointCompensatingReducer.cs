using System;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Rebuilds checkpoint state from <see cref="SagaCompensating" />.
/// </summary>
internal sealed class SagaRecoveryCheckpointCompensatingReducer
    : EventReducerBase<SagaCompensating, SagaRecoveryCheckpoint>
{
    /// <inheritdoc />
    protected override SagaRecoveryCheckpoint ReduceCore(
        SagaRecoveryCheckpoint state,
        SagaCompensating eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return SagaRecoveryCheckpointReducerState.ClearInFlight(state) with
        {
            PendingDirection = SagaExecutionDirection.Compensation,
            PendingStepIndex = eventData.FromStepIndex,
            BlockedReason = null,
        };
    }
}