using Crescent.Crescent.L2Tests.Domain.Counter.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.Crescent.L2Tests.Domain.CounterSummary.Reducers;

/// <summary>
///     Reducer that transforms <see cref="CounterIncremented" /> events into
///     <see cref="CounterSummaryProjection" /> state.
/// </summary>
internal sealed class CounterSummaryIncrementedReducer : ReducerBase<CounterIncremented, CounterSummaryProjection>
{
    /// <inheritdoc />
    protected override CounterSummaryProjection ReduceCore(
        CounterSummaryProjection state,
        CounterIncremented @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        int currentCount = state?.CurrentCount ?? 0;
        int newCount = currentCount + @event.Amount;
        int operations = (state?.TotalOperations ?? 0) + 1;
        return new()
        {
            CurrentCount = newCount,
            TotalOperations = operations,
            DisplayLabel = $"Counter: {newCount}",
            IsPositive = newCount > 0,
        };
    }
}