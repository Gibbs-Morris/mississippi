using System;

using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;


namespace Mississippi.EventSourcing.Sagas.Reducers;

/// <summary>
///     Reducer that applies <see cref="SagaCompensatingEvent" /> to a saga state implementing <see cref="ISagaState" />.
/// </summary>
/// <typeparam name="TSaga">The saga state type. Must implement <see cref="ISagaState" />.</typeparam>
/// <remarks>
///     <para>
///         This reducer transitions the saga phase to <see cref="SagaPhase.Compensating" />
///         when compensation begins after a step failure.
///     </para>
/// </remarks>
public sealed class SagaCompensatingReducer<TSaga> : EventReducerBase<SagaCompensatingEvent, TSaga>
    where TSaga : class, ISagaState
{
    /// <inheritdoc />
    protected override TSaga ReduceCore(
        TSaga state,
        SagaCompensatingEvent eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        ArgumentNullException.ThrowIfNull(state);
        return (TSaga)state.ApplyPhase(SagaPhase.Compensating);
    }
}