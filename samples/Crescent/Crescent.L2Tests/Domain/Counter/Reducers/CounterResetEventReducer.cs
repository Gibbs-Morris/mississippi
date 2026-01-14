using Crescent.Crescent.L2Tests.Domain.Counter.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.Crescent.L2Tests.Domain.Counter.Reducers;

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