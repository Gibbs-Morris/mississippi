using System;

using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;

namespace Mississippi.EventSourcing.Sagas;

/// <summary>
///     Reducer for <see cref="SagaStartedEvent" />.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
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
        return SagaStateMutator.CreateUpdated(state, (map, instance) =>
        {
            map.SetProperty(instance, nameof(ISagaState.SagaId), eventData.SagaId);
            map.SetProperty(instance, nameof(ISagaState.Phase), SagaPhase.Running);
            map.SetProperty(instance, nameof(ISagaState.LastCompletedStepIndex), -1);
            map.SetProperty(instance, nameof(ISagaState.CorrelationId), eventData.CorrelationId);
            map.SetProperty(instance, nameof(ISagaState.StartedAt), eventData.StartedAt);
            map.SetProperty(instance, nameof(ISagaState.StepHash), eventData.StepHash);
        });
    }
}
