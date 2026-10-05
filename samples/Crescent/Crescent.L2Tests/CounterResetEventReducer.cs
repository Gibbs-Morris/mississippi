using Mississippi.Tributary.Abstractions;


namespace MississippiSamples.Crescent.L2Tests;

/// <summary>
///     Reducer for <see cref="CounterReset" /> events.
/// </summary>
internal sealed class CounterResetEventReducer : EventReducerBase<CounterReset, CounterAggregate>
{
    /// <inheritdoc />
    protected override CounterAggregate ReduceCore(
        CounterAggregate state,
        CounterReset @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            Count = @event.NewValue,
            ResetCount = (state?.ResetCount ?? 0) + 1,
        };
    }
}