using System;

using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;


namespace Mississippi.EventSourcing.Sagas.Reducers;

/// <summary>
///     Reducer that applies <see cref="SagaStepCompletedEvent" /> to a saga state implementing <see cref="ISagaState" />.
/// </summary>
/// <typeparam name="TSaga">The saga state type. Must implement <see cref="ISagaState" />.</typeparam>
/// <remarks>
///     <para>
///         This reducer updates the <see cref="ISagaState.LastCompletedStepIndex" /> when a step
///         completes successfully and resets the attempt counter for the next step.
///     </para>
/// </remarks>
public sealed class SagaStepCompletedReducer<TSaga> : EventReducerBase<SagaStepCompletedEvent, TSaga>
    where TSaga : class, ISagaState
{
    /// <inheritdoc />
    protected override TSaga ReduceCore(
        TSaga state,
        SagaStepCompletedEvent eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        ArgumentNullException.ThrowIfNull(state);

        // Update step index and reset attempt counter for next step
        return (TSaga)state.ApplyStepProgress(eventData.StepOrder, 1);
    }
}