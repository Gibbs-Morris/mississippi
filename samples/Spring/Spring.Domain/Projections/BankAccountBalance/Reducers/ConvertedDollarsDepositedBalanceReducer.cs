using System;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Events;


namespace Spring.Domain.Projections.BankAccountBalance.Reducers;

/// <summary>
///     Reduces the <see cref="ConvertedDollarsDeposited" /> event to update
///     the balance in <see cref="BankAccountBalanceProjection" />.
/// </summary>
/// <remarks>
///     This reducer adds the converted GBP amount from USD deposits
///     to the account balance. The original USD amount is not stored
///     in the projection as the account balance is GBP-denominated.
/// </remarks>
internal sealed class ConvertedDollarsDepositedBalanceReducer
    : EventReducerBase<ConvertedDollarsDeposited, BankAccountBalanceProjection>
{
    /// <inheritdoc />
    protected override BankAccountBalanceProjection ReduceCore(
        BankAccountBalanceProjection state,
        ConvertedDollarsDeposited eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return state with
        {
            Balance = state.Balance + eventData.AmountGbp,
        };
    }
}