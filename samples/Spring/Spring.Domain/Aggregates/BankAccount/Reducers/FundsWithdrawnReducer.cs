using System;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Events;


namespace Spring.Domain.Aggregates.BankAccount.Reducers;

/// <summary>
///     Reducer for <see cref="FundsWithdrawn" /> events.
/// </summary>
internal sealed class FundsWithdrawnReducer : EventReducerBase<FundsWithdrawn, BankAccountAggregate>
{
    /// <inheritdoc />
    protected override BankAccountAggregate ReduceCore(
        BankAccountAggregate state,
        FundsWithdrawn @event
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