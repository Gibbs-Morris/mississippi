using System;

using Crescent.ConsoleApp.Counter.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.ConsoleApp.Counter.Reducers;

/// <summary>
///     Reducer for <see cref="CounterInitialized" /> events.
/// </summary>
internal sealed class CounterInitializedReducer : Reducer<CounterInitialized, CounterState>
{
    /// <inheritdoc />
    protected override CounterState ReduceCore(
        CounterState state,
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