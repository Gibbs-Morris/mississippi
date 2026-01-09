using System;

using Crescent.Aspire.L2Tests.Domain.Counter.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.Aspire.L2Tests.Domain.Counter.Reducers;

/// <summary>
///     Reducer for <see cref="CounterIncremented" /> events.
/// </summary>
internal sealed class CounterIncrementedReducer : Reducer<CounterIncremented, CounterAggregate>
{
    /// <inheritdoc />
    protected override CounterAggregate ReduceCore(
        CounterAggregate state,
        CounterIncremented @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            Count = state?.Count + @event.Amount ?? @event.Amount,
            IncrementCount = (state?.IncrementCount ?? 0) + 1,
        };
    }
}
