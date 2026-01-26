using System;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Events;


namespace Spring.Domain.Aggregates.BankAccount.Reducers;

/// <summary>
///     Reducer for <see cref="CashDeposited" /> events.
/// </summary>
internal sealed class CashDepositedReducer : EventReducerBase<CashDeposited, BankAccountAggregate>
{
    /// <inheritdoc />
    protected override BankAccountAggregate ReduceCore(
        BankAccountAggregate state,
        CashDeposited @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            Balance = (state?.Balance ?? 0) + @event.Amount,
            DepositCount = (state?.DepositCount ?? 0) + 1,
        };
    }
}