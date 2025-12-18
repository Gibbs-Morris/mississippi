using Mississippi.EventSourcing.Abstractions.Attributes;

using Orleans;


namespace Crescent.ConsoleApp.Counter;

/// <summary>
///     Internal state for the counter aggregate.
///     This is never exposed externally; use projections for read queries.
/// </summary>
[SnapshotName("CRESCENT", "SAMPLE", "COUNTERSTATE")]
[GenerateSerializer]
[Alias("Crescent.ConsoleApp.Counter.CounterState")]
internal sealed record CounterState
{
    /// <summary>
    ///     Gets the current count value.
    /// </summary>
    [Id(0)]
    public int Count { get; init; }

    /// <summary>
    ///     Gets the number of times the counter has been decremented.
    /// </summary>
    [Id(2)]
    public int DecrementCount { get; init; }

    /// <summary>
    ///     Gets the number of times the counter has been incremented.
    /// </summary>
    [Id(1)]
    public int IncrementCount { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the counter has been initialized.
    /// </summary>
    [Id(4)]
    public bool IsInitialized { get; init; }

    /// <summary>
    ///     Gets the number of times the counter has been reset.
    /// </summary>
    [Id(3)]
    public int ResetCount { get; init; }
}