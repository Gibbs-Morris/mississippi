using System;

using Mississippi.Tributary.Abstractions;

using MississippiSamples.Spring.Domain.Aggregates.TransactionInvestigationQueue.Events;


namespace MississippiSamples.Spring.Domain.Aggregates.TransactionInvestigationQueue.Reducers;

/// <summary>
///     Reducer for <see cref="TransactionFlagged" /> events on the aggregate state.
/// </summary>
internal sealed class TransactionFlaggedReducer
    : EventReducerBase<TransactionFlagged, TransactionInvestigationQueueAggregate>
{
    /// <inheritdoc />
    protected override TransactionInvestigationQueueAggregate ReduceCore(
        TransactionInvestigationQueueAggregate state,
        TransactionFlagged @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            TotalFlaggedCount = (state?.TotalFlaggedCount ?? 0) + 1,
        };
    }
}