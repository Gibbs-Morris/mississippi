using System;

using Crescent.ConsoleApp.Counter.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.ConsoleApp.Counter.Reducers;

/// <summary>
///     Reducer for <see cref="CounterReset" /> events.
/// </summary>
internal sealed class CounterResetReducer : Reducer<CounterReset, CounterAggregate>
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