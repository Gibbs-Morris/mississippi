using Crescent.L2Tests.Domain.Counter.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.L2Tests.Domain.Counter.Reducers;

/// <summary>
///     Reducer for <see cref="CounterInitialized" /> events.
/// </summary>
internal sealed class CounterInitializedReducer : ReducerBase<CounterInitialized, CounterAggregate>
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