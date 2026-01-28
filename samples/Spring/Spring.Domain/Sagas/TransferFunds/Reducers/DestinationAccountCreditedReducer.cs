using System;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Sagas.TransferFunds.Events;


namespace Spring.Domain.Sagas.TransferFunds.Reducers;

/// <summary>
///     Reducer for <see cref="DestinationAccountCredited" /> events.
/// </summary>
internal sealed class DestinationAccountCreditedReducer
    : EventReducerBase<DestinationAccountCredited, TransferFundsSagaState>
{
    /// <inheritdoc />
    protected override TransferFundsSagaState ReduceCore(
        TransferFundsSagaState state,
        DestinationAccountCredited @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(state);
        return state with
        {
            DestinationCredited = true,
        };
    }
}