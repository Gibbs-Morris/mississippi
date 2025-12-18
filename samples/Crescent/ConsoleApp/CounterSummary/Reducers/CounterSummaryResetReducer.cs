using System;

using Crescent.ConsoleApp.Counter.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.ConsoleApp.CounterSummary.Reducers;

/// <summary>
///     Reducer that transforms <see cref="CounterReset" /> events into
///     <see cref="CounterSummaryProjection" /> state.
/// </summary>
internal sealed class CounterSummaryResetReducer : Reducer<CounterReset, CounterSummaryProjection>
{
    /// <inheritdoc />
    protected override CounterSummaryProjection ReduceCore(
        CounterSummaryProjection state,
        CounterReset @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        int newCount = @event.NewValue;
        int operations = (state?.TotalOperations ?? 0) + 1;
        return new CounterSummaryProjection
        {
            CurrentCount = newCount,
            TotalOperations = operations,
            DisplayLabel = $"Counter: {newCount}",
            IsPositive = newCount > 0,
        };
    }
}
