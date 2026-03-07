using System;

using Mississippi.Spring.Domain.Aggregates.BankAccount.Events;
using Mississippi.Tributary.Abstractions;


namespace Mississippi.Spring.Domain.Projections.BankAccountBalance.Reducers;

/// <summary>
///     Reduces the <see cref="AccountOpened" /> event to initialize
///     the <see cref="BankAccountBalanceProjection" />.
/// </summary>
internal sealed class AccountOpenedBalanceReducer : EventReducerBase<AccountOpened, BankAccountBalanceProjection>
{
    /// <inheritdoc />
    protected override BankAccountBalanceProjection ReduceCore(
        BankAccountBalanceProjection state,
        AccountOpened eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return state with
        {
            HolderName = eventData.HolderName,
            Balance = eventData.InitialDeposit,
            IsOpen = true,
        };
    }
}