using System;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Rebuilds checkpoint state from <see cref="SagaCompleted" />.
/// </summary>
internal sealed class SagaRecoveryCheckpointCompletedReducer : EventReducerBase<SagaCompleted, SagaRecoveryCheckpoint>
{
    /// <inheritdoc />
    protected override SagaRecoveryCheckpoint ReduceCore(
        SagaRecoveryCheckpoint state,
        SagaCompleted eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return SagaRecoveryCheckpointReducerState.ClearPending(state);
    }
}