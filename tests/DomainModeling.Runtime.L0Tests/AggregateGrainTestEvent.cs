using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Aggregates.L0Tests;

/// <summary>
///     Test event for AggregateGrain tests.
/// </summary>
[EventStorageName("TEST", "AGGREGATES", "TESTEVENT")]
internal sealed record AggregateGrainTestEvent(string Value);