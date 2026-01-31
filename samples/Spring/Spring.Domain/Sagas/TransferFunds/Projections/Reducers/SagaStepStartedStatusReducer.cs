using System;

using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;
using Mississippi.EventSourcing.Sagas.Abstractions.Projections;


namespace Spring.Domain.Sagas.TransferFunds.Projections.Reducers;

/// <summary>
///     Reducer that updates <see cref="TransferFundsSagaStatusProjection" /> when a step starts.
/// </summary>
/// <remarks>
///     <para>
///         This reducer sets the current step information when a saga step begins execution.
///     </para>
/// </remarks>
public sealed class SagaStepStartedStatusReducer
    : EventReducerBase<SagaStepStartedEvent, TransferFundsSagaStatusProjection>
{
    /// <inheritdoc />
    protected override TransferFundsSagaStatusProjection ReduceCore(
        TransferFundsSagaStatusProjection state,
        SagaStepStartedEvent eventData
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(eventData);

        return state with
        {
            CurrentStep = new TransferFundsSagaStepStatus
            {
                StepName = eventData.StepName,
                StepOrder = eventData.StepOrder,
                Timestamp = eventData.Timestamp,
                Outcome = StepOutcome.Started.ToString(),
            },
        };
    }
}
