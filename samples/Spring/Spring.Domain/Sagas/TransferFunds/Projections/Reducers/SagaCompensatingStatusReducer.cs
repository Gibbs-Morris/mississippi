using System;

using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;
using Mississippi.EventSourcing.Sagas.Abstractions.Projections;


namespace Spring.Domain.Sagas.TransferFunds.Projections.Reducers;

/// <summary>
///     Reducer that updates <see cref="TransferFundsSagaStatusProjection" /> when compensation begins.
/// </summary>
/// <remarks>
///     <para>
///         This reducer sets the phase to Compensating when the saga starts rolling back
///         due to a step failure.
///     </para>
/// </remarks>
public sealed class SagaCompensatingStatusReducer
    : EventReducerBase<SagaCompensatingEvent, TransferFundsSagaStatusProjection>
{
    /// <inheritdoc />
    protected override TransferFundsSagaStatusProjection ReduceCore(
        TransferFundsSagaStatusProjection state,
        SagaCompensatingEvent eventData
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(eventData);

        return state with
        {
            Phase = SagaPhase.Compensating.ToString(),
            CurrentStep = null,
        };
    }
}
