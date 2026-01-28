using System;

using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;


namespace Mississippi.EventSourcing.Sagas.Reducers;

/// <summary>
///     Reducer that applies <see cref="SagaStepRetryEvent" /> to a saga state implementing <see cref="ISagaState" />.
/// </summary>
/// <typeparam name="TSaga">The saga state type. Must implement <see cref="ISagaState" />.</typeparam>
/// <remarks>
///     <para>
///         This reducer updates the <see cref="ISagaState.CurrentStepAttempt" /> when a step
///         is retried under the <see cref="CompensationStrategy.RetryThenCompensate" /> strategy.
///     </para>
/// </remarks>
public sealed class SagaStepRetryReducer<TSaga> : EventReducerBase<SagaStepRetryEvent, TSaga>
    where TSaga : class, ISagaState
{
    /// <inheritdoc />
    protected override TSaga ReduceCore(
        TSaga state,
        SagaStepRetryEvent eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        ArgumentNullException.ThrowIfNull(state);

        // Keep the same last completed index, just update the attempt number
        return (TSaga)state.ApplyStepProgress(state.LastCompletedStepIndex, eventData.AttemptNumber);
    }
}