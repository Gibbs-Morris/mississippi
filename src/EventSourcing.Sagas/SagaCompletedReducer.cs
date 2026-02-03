using System;

using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;

namespace Mississippi.EventSourcing.Sagas;

/// <summary>
///     Reducer for <see cref="SagaCompleted" />.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public sealed class SagaCompletedReducer<TSaga> : EventReducerBase<SagaCompleted, TSaga>
    where TSaga : class, ISagaState
{
    /// <inheritdoc />
    protected override TSaga ReduceCore(
        TSaga state,
        SagaCompleted eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return SagaStateMutator.CreateUpdated(state, (map, instance) =>
            map.SetProperty(instance, nameof(ISagaState.Phase), SagaPhase.Completed));
    }
}
