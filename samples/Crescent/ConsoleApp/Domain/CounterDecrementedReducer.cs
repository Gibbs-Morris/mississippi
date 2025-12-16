using System;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.ConsoleApp.Domain;

/// <summary>
///     Reducer for <see cref="CounterDecremented" /> events.
/// </summary>
internal sealed class CounterDecrementedReducer : Reducer<CounterDecremented, CounterState>
{
    /// <inheritdoc />
    protected override CounterState ReduceCore(
        CounterState state,
        CounterDecremented @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);

        return (state ?? new()) with
        {
            Count = state?.Count - @event.Amount ?? -@event.Amount,
            DecrementCount = (state?.DecrementCount ?? 0) + 1,
        };
    }
}
