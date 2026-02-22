namespace Mississippi.EventSourcing.Aggregates.Api.L0Tests;

/// <summary>
///     Test aggregate record for unit tests.
/// </summary>
/// <param name="Id">The aggregate identifier.</param>
internal sealed record TestAggregate(string Id);