using Mississippi.Brooks.Abstractions.Attributes;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Test event for AggregateGrain tests.
/// </summary>
[EventStorageName("TEST", "AGGREGATES", "TESTEVENT")]
internal sealed record AggregateGrainTestEvent(string Value);