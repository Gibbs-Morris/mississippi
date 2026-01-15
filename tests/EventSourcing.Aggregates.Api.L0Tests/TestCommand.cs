namespace Mississippi.EventSourcing.Aggregates.Api.L0Tests;

/// <summary>
///     Test command record for unit tests.
/// </summary>
/// <param name="Value">The command value.</param>
internal sealed record TestCommand(string Value);