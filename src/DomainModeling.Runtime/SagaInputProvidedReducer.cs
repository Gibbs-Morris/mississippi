using System;

using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas;

/// <summary>
///     Reducer for <see cref="SagaInputProvided{TInput}" />.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
/// <typeparam name="TInput">The saga input type.</typeparam>
public sealed class SagaInputProvidedReducer<TSaga, TInput> : EventReducerBase<SagaInputProvided<TInput>, TSaga>
    where TSaga : class, ISagaState
{
    /// <inheritdoc />
    protected override TSaga ReduceCore(
        TSaga state,
        SagaInputProvided<TInput> eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return SagaStateMutator.CreateUpdated(
            state,
            (
                map,
                instance
            ) =>
            {
                map.TrySetProperty(instance, "Input", eventData.Input);
            });
    }
}