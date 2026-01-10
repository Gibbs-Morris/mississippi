using Crescent.L2Tests.Domain.Counter.Events;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Crescent.L2Tests.Domain.CounterSummary.Reducers;

/// <summary>
///     Reducer that transforms <see cref="CounterInitialized" /> events into
///     <see cref="CounterSummaryProjection" /> state.
/// </summary>
internal sealed class CounterSummaryInitializedReducer : Reducer<CounterInitialized, CounterSummaryProjection>
{
    /// <inheritdoc />
    protected override CounterSummaryProjection ReduceCore(
        CounterSummaryProjection state,
        CounterInitialized @event
    )
    {
        ArgumentNullException.ThrowIfNull(@event);
        int newCount = @event.InitialValue;
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