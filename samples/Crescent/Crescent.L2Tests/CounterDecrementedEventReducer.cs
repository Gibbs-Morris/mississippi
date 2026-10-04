using Mississippi.Tributary.Abstractions;


namespace MississippiSamples.Crescent.L2Tests;

/// <summary>
///     Reducer for <see cref="CounterDecremented" /> events.
/// </summary>
internal sealed class CounterDecrementedEventReducer : EventReducerBase<CounterDecremented, CounterAggregate>
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