using System;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Sagas.TransferFunds.Events;


namespace Spring.Domain.Sagas.TransferFunds.Reducers;

/// <summary>
///     Reducer for <see cref="TransferInitiated" /> events.
/// </summary>
/// <remarks>
///     <para>
///         Applies the initial transfer data (source account, destination account, amount)
///         from the <see cref="TransferInitiated" /> event to the saga state.
///     </para>
/// </remarks>
internal sealed class TransferInitiatedReducer : EventReducerBase<TransferInitiated, TransferFundsSagaState>
{
    /// <inheritdoc />
    protected override TransferFundsSagaState ReduceCore(
        TransferFundsSagaState state,
        TransferInitiated @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            SourceAccountId = @event.SourceAccountId,
            DestinationAccountId = @event.DestinationAccountId,
            Amount = @event.Amount,
        };
    }
}
