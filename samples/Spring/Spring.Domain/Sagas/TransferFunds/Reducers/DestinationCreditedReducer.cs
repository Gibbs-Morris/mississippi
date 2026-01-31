using System;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Sagas.TransferFunds.Events;


namespace Spring.Domain.Sagas.TransferFunds.Reducers;

/// <summary>
///     Reducer for <see cref="DestinationCredited" /> events.
/// </summary>
/// <remarks>
///     <para>
///         Sets <see cref="TransferFundsSagaState.DestinationCredited" /> to <c>true</c>
///         when a <see cref="DestinationCredited" /> event is applied.
///     </para>
/// </remarks>
internal sealed class DestinationCreditedReducer : EventReducerBase<DestinationCredited, TransferFundsSagaState>
{
    /// <inheritdoc />
    protected override TransferFundsSagaState ReduceCore(
        TransferFundsSagaState state,
        DestinationCredited @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            DestinationCredited = true,
        };
    }
}