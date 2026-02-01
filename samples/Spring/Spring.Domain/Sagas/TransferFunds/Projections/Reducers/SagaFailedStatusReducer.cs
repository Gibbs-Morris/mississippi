using System;

using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;


namespace Spring.Domain.Sagas.TransferFunds.Projections.Reducers;

/// <summary>
///     Reducer that updates <see cref="TransferFundsSagaStatusProjection" /> when the saga fails.
/// </summary>
/// <remarks>
///     <para>
///         This reducer sets the phase to Failed, records the failure reason, and completion time.
///     </para>
/// </remarks>
public sealed class SagaFailedStatusReducer : EventReducerBase<SagaFailedEvent, TransferFundsSagaStatusProjection>
{
    /// <inheritdoc />
    protected override TransferFundsSagaStatusProjection ReduceCore(
        TransferFundsSagaStatusProjection state,
        SagaFailedEvent eventData
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(eventData);
        return state with
        {
            Phase = SagaPhase.Failed.ToString(),
            FailureReason = eventData.Reason,
            CompletedAt = eventData.Timestamp,
            CurrentStep = null,
        };
    }
}