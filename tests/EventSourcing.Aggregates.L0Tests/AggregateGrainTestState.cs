using Mississippi.EventSourcing.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Aggregates.Tests;

/// <summary>
///     Test state for AggregateGrain tests.
/// </summary>
[SnapshotName("MISSISSIPPI", "TESTS", "AGGREGATEGRAINTEST")]
internal sealed record AggregateGrainTestState(int Count, string LastValue);