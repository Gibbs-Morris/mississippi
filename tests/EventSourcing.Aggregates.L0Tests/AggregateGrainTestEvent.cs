using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Aggregates.Tests;

/// <summary>
///     Test event for AggregateGrain tests.
/// </summary>
[EventStorageName("TEST", "AGGREGATES", "TESTEVENT", 1)]
internal sealed record AggregateGrainTestEvent(string Value);