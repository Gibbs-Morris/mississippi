namespace Mississippi.EventSourcing.Sagas.L0Tests.Helpers;

/// <summary>
///     Test business event for saga tests.
/// </summary>
/// <param name="Message">The event message.</param>
internal sealed record TestBusinessEvent(string Message);