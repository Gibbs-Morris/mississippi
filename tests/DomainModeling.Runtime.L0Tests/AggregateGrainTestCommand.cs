namespace Mississippi.EventSourcing.Aggregates.L0Tests;

/// <summary>
///     Test command for AggregateGrain tests.
/// </summary>
internal sealed record AggregateGrainTestCommand(string Value);