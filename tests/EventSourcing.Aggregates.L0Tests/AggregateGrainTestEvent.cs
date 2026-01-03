using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Aggregates.Tests;

/// <summary>
///     Test event for AggregateGrain tests.
/// </summary>
[EventStorageName("TEST", "AGGREGATES", "TESTEVENT")]
internal sealed record AggregateGrainTestEvent(string Value);