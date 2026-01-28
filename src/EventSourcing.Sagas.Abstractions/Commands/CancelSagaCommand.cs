namespace Mississippi.EventSourcing.Sagas.Abstractions.Commands;

/// <summary>
///     Command to cancel a running saga and trigger compensation.
/// </summary>
/// <param name="Reason">The reason for cancellation.</param>
public sealed record CancelSagaCommand(string Reason);