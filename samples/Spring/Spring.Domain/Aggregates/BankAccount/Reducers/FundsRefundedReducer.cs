using System;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Events;


namespace Spring.Domain.Aggregates.BankAccount.Reducers;

/// <summary>
///     Reducer for <see cref="FundsRefunded" /> events.
/// </summary>
/// <remarks>
///     Refunds restore funds to the source account after a failed transfer,
///     incrementing the balance and decrementing the outgoing transfer count.
/// </remarks>
internal sealed class FundsRefundedReducer : EventReducerBase<FundsRefunded, BankAccountAggregate>
{
    /// <inheritdoc />
    protected override BankAccountAggregate ReduceCore(
        BankAccountAggregate state,
        FundsRefunded @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            Balance = (state?.Balance ?? 0) + @event.Amount,

            // Decrement outgoing transfer count since the transfer was rolled back
            OutgoingTransferCount = Math.Max(0, (state?.OutgoingTransferCount ?? 0) - 1),
        };
    }
}