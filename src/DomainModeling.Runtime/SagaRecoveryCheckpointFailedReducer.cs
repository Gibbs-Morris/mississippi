using System;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Rebuilds checkpoint state from <see cref="SagaFailed" />.
/// </summary>
internal sealed class SagaRecoveryCheckpointFailedReducer : EventReducerBase<SagaFailed, SagaRecoveryCheckpoint>
{
    /// <inheritdoc />
    protected override SagaRecoveryCheckpoint ReduceCore(
        SagaRecoveryCheckpoint state,
        SagaFailed eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return SagaRecoveryCheckpointReducerState.ClearPending(state);
    }
}