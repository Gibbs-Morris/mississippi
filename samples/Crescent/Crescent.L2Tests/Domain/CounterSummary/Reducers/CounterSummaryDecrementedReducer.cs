using Crescent.L2Tests.Domain.Counter.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.L2Tests.Domain.CounterSummary.Reducers;

/// <summary>
///     Reducer that transforms <see cref="CounterDecremented" /> events into
///     <see cref="CounterSummaryProjection" /> state.
/// </summary>
internal sealed class CounterSummaryDecrementedReducer : ReducerBase<CounterDecremented, CounterSummaryProjection>
{
    /// <inheritdoc />
    protected override CounterSummaryProjection ReduceCore(
        CounterSummaryProjection state,
        CounterDecremented @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        int currentCount = state?.CurrentCount ?? 0;
        int newCount = currentCount - @event.Amount;
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