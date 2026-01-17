using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Crescent.Crescent.L2Tests.Domain.Counter;

/// <summary>
///     Internal aggregate state for the counter.
///     This is never exposed externally; use projections for read queries.
/// </summary>
[BrookName("CRESCENT", "SAMPLE", "COUNTER")]
[SnapshotStorageName("CRESCENT", "SAMPLE", "COUNTERSTATE")]
[GenerateSerializer]
[Alias("Crescent.L2Tests.Domain.Counter.CounterAggregate")]
internal sealed record CounterAggregate
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