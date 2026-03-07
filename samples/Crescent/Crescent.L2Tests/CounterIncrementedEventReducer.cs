using Mississippi.Tributary.Abstractions;


namespace Mississippi.Crescent.L2Tests;

/// <summary>
///     Reducer for <see cref="CounterIncremented" /> events.
/// </summary>
internal sealed class CounterIncrementedEventReducer : EventReducerBase<CounterIncremented, CounterAggregate>
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