using System;

using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas;

/// <summary>
///     Reducer for <see cref="SagaStepCompleted" />.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public sealed class SagaStepCompletedReducer<TSaga> : EventReducerBase<SagaStepCompleted, TSaga>
    where TSaga : class, ISagaState
{
    /// <inheritdoc />
    protected override TSaga ReduceCore(
        TSaga state,
        SagaStepCompleted eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return SagaStateMutator.CreateUpdated(
            state,
            (
                map,
                instance
            ) => map.SetProperty(instance, nameof(ISagaState.LastCompletedStepIndex), eventData.StepIndex));
    }
}