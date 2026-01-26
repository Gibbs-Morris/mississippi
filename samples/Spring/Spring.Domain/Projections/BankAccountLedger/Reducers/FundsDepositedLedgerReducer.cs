using System;
using System.Collections.Immutable;
using System.Linq;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Events;


namespace Spring.Domain.Projections.BankAccountLedger.Reducers;

/// <summary>
///     Reduces the <see cref="FundsDeposited" /> event to add
///     a GBP deposit entry to <see cref="BankAccountLedgerProjection" />.
/// </summary>
internal sealed class FundsDepositedLedgerReducer : EventReducerBase<FundsDeposited, BankAccountLedgerProjection>
{
    /// <inheritdoc />
    protected override BankAccountLedgerProjection ReduceCore(
        BankAccountLedgerProjection state,
        FundsDeposited eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        long newSequence = state.CurrentSequence + 1;
        LedgerEntry entry = new()
        {
            EntryType = LedgerEntryType.Deposit,
            Amount = eventData.Amount,
            Sequence = newSequence,
        };
        ImmutableArray<LedgerEntry> entries = state.Entries.Prepend(entry)
            .Take(BankAccountLedgerProjection.MaxEntries)
            .ToImmutableArray();
        return state with
        {
            Entries = entries,
            CurrentSequence = newSequence,
        };
    }
}