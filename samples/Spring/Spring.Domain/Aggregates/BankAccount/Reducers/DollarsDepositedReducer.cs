using System;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Events;


namespace Spring.Domain.Aggregates.BankAccount.Reducers;

/// <summary>
///     Reducer for <see cref="DollarsDeposited" /> events.
/// </summary>
/// <remarks>
///     <para>
///         This reducer only increments the deposit count. The actual balance
///         update happens when the <see cref="ConvertedDollarsDeposited" /> event
///         is processed (after the effect fetches the exchange rate).
///     </para>
///     <para>
///         Note: If the currency conversion effect fails (e.g., network error),
///         the deposit count will be incremented but the balance unchanged.
///         This is intentional for the demo—production may need compensating events.
///     </para>
/// </remarks>
internal sealed class DollarsDepositedReducer : EventReducerBase<DollarsDeposited, BankAccountAggregate>
{
    /// <inheritdoc />
    protected override BankAccountAggregate ReduceCore(
        BankAccountAggregate state,
        DollarsDeposited @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);

        // Only increment deposit count; balance is updated by ConvertedDollarsDepositedReducer
        return (state ?? new()) with
        {
            DepositCount = (state?.DepositCount ?? 0) + 1,
        };
    }
}