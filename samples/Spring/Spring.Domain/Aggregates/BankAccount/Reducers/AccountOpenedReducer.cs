using System;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Events;


namespace Spring.Domain.Aggregates.BankAccount.Reducers;

/// <summary>
///     Reducer for <see cref="AccountOpened" /> events.
/// </summary>
internal sealed class AccountOpenedReducer : EventReducerBase<AccountOpened, BankAccountAggregate>
{
    /// <inheritdoc />
    protected override BankAccountAggregate ReduceCore(
        BankAccountAggregate state,
        AccountOpened @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            IsOpen = true,
            HolderName = @event.HolderName,
            Balance = @event.InitialDeposit,
        };
    }
}