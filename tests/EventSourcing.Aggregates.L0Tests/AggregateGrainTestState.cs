namespace Mississippi.EventSourcing.Aggregates.Tests;

/// <summary>
///     Test state for AggregateGrain tests.
/// </summary>
internal sealed record AggregateGrainTestState(int Count, string LastValue);
