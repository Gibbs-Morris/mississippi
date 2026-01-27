using System;
using System.Collections.Immutable;
using System.Linq;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Spring.Domain.Aggregates.TransactionInvestigationQueue.Events;


namespace Spring.Domain.Projections.FlaggedTransactions.Reducers;

/// <summary>
///     Reduces the <see cref="TransactionFlagged" /> event to add
///     a flagged transaction entry to <see cref="FlaggedTransactionsProjection" />.
/// </summary>
internal sealed class TransactionFlaggedProjectionReducer
    : EventReducerBase<TransactionFlagged, FlaggedTransactionsProjection>
{
    /// <inheritdoc />
    protected override FlaggedTransactionsProjection ReduceCore(
        FlaggedTransactionsProjection state,
        TransactionFlagged eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        long newSequence = state.CurrentSequence + 1;
        FlaggedTransaction entry = new()
        {
            AccountId = eventData.AccountId,
            Amount = eventData.Amount,
            OriginalTimestamp = eventData.OriginalTimestamp,
            FlaggedTimestamp = eventData.FlaggedTimestamp,
            Sequence = newSequence,
        };
        ImmutableArray<FlaggedTransaction> entries = state.Entries.Prepend(entry)
            .Take(FlaggedTransactionsProjection.MaxEntries)
            .ToImmutableArray();
        return state with
        {
            Entries = entries,
            CurrentSequence = newSequence,
        };
    }
}