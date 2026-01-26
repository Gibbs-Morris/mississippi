using System;
using System.Collections.Immutable;
using System.Linq;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Events;


namespace Spring.Domain.Projections.BankAccountLedger.Reducers;

/// <summary>
///     Reduces the <see cref="FundsWithdrawn" /> event to add
///     a withdrawal entry to <see cref="BankAccountLedgerProjection" />.
/// </summary>
internal sealed class FundsWithdrawnLedgerReducer : EventReducerBase<FundsWithdrawn, BankAccountLedgerProjection>
{
    /// <inheritdoc />
    protected override BankAccountLedgerProjection ReduceCore(
        BankAccountLedgerProjection state,
        FundsWithdrawn eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        long newSequence = state.CurrentSequence + 1;
        LedgerEntry entry = new()
        {
            EntryType = LedgerEntryType.Withdrawal,
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