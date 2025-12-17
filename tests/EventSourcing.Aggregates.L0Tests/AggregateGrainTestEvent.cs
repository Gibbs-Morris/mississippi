using Mississippi.EventSourcing.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Aggregates.Tests;

/// <summary>
///     Test event for AggregateGrain tests.
/// </summary>
[EventName("TEST", "AGGREGATES", "TESTEVENT")]
internal sealed record AggregateGrainTestEvent(string Value);