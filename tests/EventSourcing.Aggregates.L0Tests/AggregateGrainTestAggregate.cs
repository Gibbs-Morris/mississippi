using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Aggregates.L0Tests;

/// <summary>
///     Test state for AggregateGrain tests.
/// </summary>
[SnapshotStorageName("MISSISSIPPI", "TESTS", "AGGREGATEGRAINTEST")]
internal sealed record AggregateGrainTestAggregate(int Count, string LastValue);