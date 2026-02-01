namespace Mississippi.EventSourcing.Sagas.L0Tests.Helpers;

/// <summary>
///     Test input record for saga tests.
/// </summary>
/// <param name="OrderId">The order identifier.</param>
internal sealed record TestSagaInput(string OrderId);