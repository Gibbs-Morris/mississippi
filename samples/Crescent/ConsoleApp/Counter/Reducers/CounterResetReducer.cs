using System;

using Crescent.ConsoleApp.Counter.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.ConsoleApp.Counter.Reducers;

/// <summary>
///     Reducer for <see cref="CounterReset" /> events.
/// </summary>
internal sealed class CounterResetReducer
    : Reducer<CounterReset, CounterState>
{
    /// <inheritdoc />
    protected override CounterState ReduceCore(
        CounterState state,
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
