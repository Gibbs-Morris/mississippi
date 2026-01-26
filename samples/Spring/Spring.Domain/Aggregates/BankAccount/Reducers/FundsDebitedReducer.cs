using System;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Events;

namespace Spring.Domain.Aggregates.BankAccount.Reducers;

/// <summary>
///     Reducer for <see cref="FundsDebited" /> events.
/// </summary>
/// <remarks>
///     Debits decrease the balance and increment the outgoing transfer count.
/// </remarks>
internal sealed class FundsDebitedReducer : EventReducerBase<FundsDebited, BankAccountAggregate>
{
    /// <inheritdoc />
    protected override BankAccountAggregate ReduceCore(
        BankAccountAggregate state,
        FundsDebited @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            Balance = (state?.Balance ?? 0) - @event.Amount,
            OutgoingTransferCount = (state?.OutgoingTransferCount ?? 0) + 1,
        };
    }
}
