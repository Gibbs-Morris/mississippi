using Mississippi.Brooks.Abstractions.Attributes;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Test state for AggregateGrain tests.
/// </summary>
[BrookName("TEST", "AGGREGATES", "BROOK")]
[SnapshotStorageName("MISSISSIPPI", "TESTS", "AGGREGATEGRAINTEST")]
internal sealed record AggregateGrainTestAggregate(int Count, string LastValue);