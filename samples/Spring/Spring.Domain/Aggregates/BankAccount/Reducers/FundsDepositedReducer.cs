using System;

using Mississippi.Tributary.Abstractions;

using MississippiSamples.Spring.Domain.Aggregates.BankAccount.Events;


namespace MississippiSamples.Spring.Domain.Aggregates.BankAccount.Reducers;

/// <summary>
///     Reducer for <see cref="FundsDeposited" /> events.
/// </summary>
internal sealed class FundsDepositedReducer : EventReducerBase<FundsDeposited, BankAccountAggregate>
{
    /// <inheritdoc />
    protected override BankAccountAggregate ReduceCore(
        BankAccountAggregate state,
        FundsDeposited @event
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