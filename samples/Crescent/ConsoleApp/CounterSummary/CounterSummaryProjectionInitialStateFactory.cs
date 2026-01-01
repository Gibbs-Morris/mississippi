using Mississippi.EventSourcing.Snapshots.Abstractions;


namespace Crescent.ConsoleApp.CounterSummary;

/// <summary>
///     Factory for creating the initial state of <see cref="CounterSummaryProjection" /> snapshots.
/// </summary>
internal sealed class CounterSummaryProjectionInitialStateFactory : IInitialStateFactory<CounterSummaryProjection>
{
    /// <inheritdoc />
    public CounterSummaryProjection Create() =>
        new()
        {
            CurrentCount = 0,
            TotalOperations = 0,
            DisplayLabel = string.Empty,
            IsPositive = false,
        };
}