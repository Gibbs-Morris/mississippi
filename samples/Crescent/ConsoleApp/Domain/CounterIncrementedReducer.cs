using System;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.ConsoleApp.Domain;

/// <summary>
///     Reducer for <see cref="CounterIncremented" /> events.
/// </summary>
internal sealed class CounterIncrementedReducer : Reducer<CounterIncremented, CounterState>
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