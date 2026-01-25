using System;
using System.Collections.Immutable;
using System.Linq;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Events;


namespace Spring.Domain.Projections.BankAccountLedger.Reducers;

/// <summary>
///     Reduces the <see cref="ConvertedDollarsDeposited" /> event to add
///     a USD deposit entry to <see cref="BankAccountLedgerProjection" />.
/// </summary>
internal sealed class ConvertedDollarsDepositedLedgerReducer
    : EventReducerBase<ConvertedDollarsDeposited, BankAccountLedgerProjection>
{
    /// <inheritdoc />
    protected override BankAccountLedgerProjection ReduceCore(
        BankAccountLedgerProjection state,
        ConvertedDollarsDeposited eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        long newSequence = state.CurrentSequence + 1;
        LedgerEntry entry = new()
        {
            EntryType = LedgerEntryType.DepositUsd,
            AmountGbp = eventData.AmountGbp,
            AmountUsd = eventData.AmountUsd,
            ExchangeRate = eventData.ExchangeRate,
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