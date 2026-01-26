using System;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Events;


namespace Spring.Domain.Aggregates.BankAccount.Reducers;

/// <summary>
///     Reducer for <see cref="CashWithdrawn" /> events.
/// </summary>
internal sealed class CashWithdrawnReducer : EventReducerBase<CashWithdrawn, BankAccountAggregate>
{
    /// <inheritdoc />
    protected override BankAccountAggregate ReduceCore(
        BankAccountAggregate state,
        CashWithdrawn @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            Balance = (state?.Balance ?? 0) - @event.Amount,
            WithdrawalCount = (state?.WithdrawalCount ?? 0) + 1,
        };
    }
}