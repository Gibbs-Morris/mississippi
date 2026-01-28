using System;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Events;


namespace Spring.Domain.Aggregates.BankAccount.Reducers;

/// <summary>
///     Reducer for <see cref="FundsReceived" /> events.
/// </summary>
/// <remarks>
///     Received funds increase the balance and increment the incoming transfer count.
/// </remarks>
internal sealed class FundsReceivedReducer : EventReducerBase<FundsReceived, BankAccountAggregate>
{
    /// <inheritdoc />
    protected override BankAccountAggregate ReduceCore(
        BankAccountAggregate state,
        FundsReceived @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            Balance = (state?.Balance ?? 0) + @event.Amount,
            IncomingTransferCount = (state?.IncomingTransferCount ?? 0) + 1,
        };
    }
}