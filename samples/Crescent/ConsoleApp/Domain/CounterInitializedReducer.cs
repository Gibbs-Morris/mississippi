using System;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.ConsoleApp.Domain;

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