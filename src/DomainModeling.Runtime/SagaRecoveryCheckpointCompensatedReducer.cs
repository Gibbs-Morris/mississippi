using System;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Rebuilds checkpoint state from <see cref="SagaCompensated" />.
/// </summary>
internal sealed class SagaRecoveryCheckpointCompensatedReducer : EventReducerBase<SagaCompensated, SagaRecoveryCheckpoint>
{
    /// <inheritdoc />
    protected override SagaRecoveryCheckpoint ReduceCore(
        SagaRecoveryCheckpoint state,
        SagaCompensated eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return SagaRecoveryCheckpointReducerState.ClearPending(state);
    }
}