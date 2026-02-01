using System;

using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;


namespace Spring.Domain.Sagas.TransferFunds.Projections.Reducers;

/// <summary>
///     Reducer that updates <see cref="TransferFundsSagaStatusProjection" /> when a step completes.
/// </summary>
/// <remarks>
///     <para>
///         This reducer moves the current step to the completed steps list and clears the current step.
///     </para>
/// </remarks>
public sealed class SagaStepCompletedStatusReducer
    : EventReducerBase<SagaStepCompletedEvent, TransferFundsSagaStatusProjection>
{
    /// <inheritdoc />
    protected override TransferFundsSagaStatusProjection ReduceCore(
        TransferFundsSagaStatusProjection state,
        SagaStepCompletedEvent eventData
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(eventData);
        TransferFundsSagaStepStatus completedStep = new()
        {
            StepName = eventData.StepName,
            StepOrder = eventData.StepOrder,
            Timestamp = eventData.Timestamp,
            Outcome = StepOutcome.Succeeded.ToString(),
        };
        return state with
        {
            CurrentStep = null,
            CompletedSteps = state.CompletedSteps.Add(completedStep),
        };
    }
}