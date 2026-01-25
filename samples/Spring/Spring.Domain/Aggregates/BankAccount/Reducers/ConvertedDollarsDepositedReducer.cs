using System;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Events;


namespace Spring.Domain.Aggregates.BankAccount.Reducers;

/// <summary>
///     Reducer for <see cref="ConvertedDollarsDeposited" /> events.
/// </summary>
/// <remarks>
///     This reducer adds the converted GBP amount to the account balance.
///     It is triggered after the <c>CurrencyConversionEffect</c> fetches
///     the exchange rate and yields this event.
/// </remarks>
internal sealed class ConvertedDollarsDepositedReducer
    : EventReducerBase<ConvertedDollarsDeposited, BankAccountAggregate>
{
    /// <inheritdoc />
    protected override BankAccountAggregate ReduceCore(
        BankAccountAggregate state,
        ConvertedDollarsDeposited @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);

        // Add the converted GBP amount to the balance
        return (state ?? new()) with
        {
            Balance = (state?.Balance ?? 0) + @event.AmountGbp,
        };
    }
}