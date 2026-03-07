using Mississippi.Tributary.Abstractions;


namespace Mississippi.Crescent.L2Tests;

/// <summary>
///     Reducer for <see cref="CounterInitialized" /> events.
/// </summary>
internal sealed class CounterInitializedEventReducer : EventReducerBase<CounterInitialized, CounterAggregate>
{
    /// <inheritdoc />
    protected override CounterAggregate ReduceCore(
        CounterAggregate state,
        CounterInitialized @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        return (state ?? new()) with
        {
            Count = @event.InitialValue,
            IsInitialized = true,
        };
    }
}