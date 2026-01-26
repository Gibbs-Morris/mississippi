using System;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Events;

namespace Spring.Domain.Projections.BankAccountBalance.Reducers;

/// <summary>
///     Reduces the <see cref="FundsDebited" /> event to update
///     the balance in <see cref="BankAccountBalanceProjection" />.
/// </summary>
internal sealed class FundsDebitedBalanceReducer : EventReducerBase<FundsDebited, BankAccountBalanceProjection>
{
    /// <inheritdoc />
    protected override BankAccountBalanceProjection ReduceCore(
        BankAccountBalanceProjection state,
        FundsDebited eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return state with
        {
            Balance = state.Balance - eventData.Amount,
        };
    }
}
