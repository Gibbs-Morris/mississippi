using Crescent.L2Tests.Domain.Counter.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.L2Tests.Domain.Counter.Reducers;

/// <summary>
///     Reducer for <see cref="CounterDecremented" /> events.
/// </summary>
internal sealed class CounterDecrementedReducer : ReducerBase<CounterDecremented, CounterAggregate>
{
    /// <inheritdoc />
    protected override CounterAggregate ReduceCore(
        CounterAggregate state,
        CounterDecremented @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            Count = (state?.Count ?? 0) - @event.Amount,
            DecrementCount = (state?.DecrementCount ?? 0) + 1,
        };
    }
}