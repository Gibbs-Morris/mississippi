using System;

using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;


namespace Spring.Domain.Sagas.TransferFunds.Projections.Reducers;

/// <summary>
///     Reducer that initializes <see cref="TransferFundsSagaStatusProjection" /> when a saga starts.
/// </summary>
/// <remarks>
///     <para>
///         This reducer captures the saga ID, type, and start time when the saga begins execution.
///     </para>
/// </remarks>
public sealed class SagaStartedStatusReducer : EventReducerBase<SagaStartedEvent, TransferFundsSagaStatusProjection>
{
    /// <inheritdoc />
    protected override TransferFundsSagaStatusProjection ReduceCore(
        TransferFundsSagaStatusProjection state,
        SagaStartedEvent eventData
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(eventData);
        return state with
        {
            SagaId = eventData.SagaId,
            SagaType = eventData.SagaType,
            Phase = SagaPhase.Running.ToString(),
            StartedAt = eventData.Timestamp,
        };
    }
}