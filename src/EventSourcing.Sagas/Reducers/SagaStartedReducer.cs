using System;

using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;


namespace Mississippi.EventSourcing.Sagas.Reducers;

/// <summary>
///     Reducer that applies <see cref="SagaStartedEvent" /> to a saga state implementing <see cref="ISagaState" />.
/// </summary>
/// <typeparam name="TSaga">The saga state type. Must implement <see cref="ISagaState" />.</typeparam>
/// <remarks>
///     <para>
///         This reducer populates the <see cref="ISagaState" /> identity and tracking fields
///         when a saga is first started via the <see cref="ISagaState.ApplySagaStarted" /> method.
///     </para>
/// </remarks>
public sealed class SagaStartedReducer<TSaga> : EventReducerBase<SagaStartedEvent, TSaga>
    where TSaga : class, ISagaState
{
    /// <inheritdoc />
    protected override TSaga ReduceCore(
        TSaga state,
        SagaStartedEvent eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        ArgumentNullException.ThrowIfNull(state);

        // Parse the SagaId from the event (stored as string for serialization flexibility)
        Guid sagaId = Guid.TryParse(eventData.SagaId, out Guid parsedId) ? parsedId : Guid.Empty;
        return (TSaga)state.ApplySagaStarted(sagaId, eventData.CorrelationId, eventData.StepHash, eventData.Timestamp);
    }
}