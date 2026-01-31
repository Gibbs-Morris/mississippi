using System;

using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;


namespace Spring.Domain.Sagas.TransferFunds.Projections.Reducers;

/// <summary>
///     Reducer that updates <see cref="TransferFundsSagaStatusProjection" /> when the saga completes.
/// </summary>
/// <remarks>
///     <para>
///         This reducer sets the phase to Completed and records the completion time.
///     </para>
/// </remarks>
public sealed class SagaCompletedStatusReducer : EventReducerBase<SagaCompletedEvent, TransferFundsSagaStatusProjection>
{
    /// <inheritdoc />
    protected override TransferFundsSagaStatusProjection ReduceCore(
        TransferFundsSagaStatusProjection state,
        SagaCompletedEvent eventData
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(eventData);
        return state with
        {
            Phase = SagaPhase.Completed.ToString(),
            CompletedAt = eventData.Timestamp,
            CurrentStep = null,
        };
    }
}