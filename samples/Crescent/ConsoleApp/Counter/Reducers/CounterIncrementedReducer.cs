using System;

using Crescent.ConsoleApp.Counter.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.ConsoleApp.Counter.Reducers;

/// <summary>
///     Reducer for <see cref="CounterIncremented" /> events.
/// </summary>
internal sealed class CounterIncrementedReducer
    : Reducer<CounterIncremented, CounterState>
{
    /// <inheritdoc />
    protected override CounterState ReduceCore(
        CounterState state,
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
