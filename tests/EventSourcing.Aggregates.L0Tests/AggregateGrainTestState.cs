using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Aggregates.Tests;

/// <summary>
///     Test state for AggregateGrain tests.
/// </summary>
[SnapshotStorageName("MISSISSIPPI", "TESTS", "AGGREGATEGRAINTEST")]
internal sealed record AggregateGrainTestState(int Count, string LastValue);