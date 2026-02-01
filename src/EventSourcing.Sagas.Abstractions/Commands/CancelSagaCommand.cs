using Orleans;


namespace Mississippi.EventSourcing.Sagas.Abstractions.Commands;

/// <summary>
///     Command to cancel a running saga and trigger compensation.
/// </summary>
/// <param name="Reason">The reason for cancellation.</param>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Sagas.Abstractions.Commands.CancelSagaCommand")]
public sealed record CancelSagaCommand([property: Id(0)] string Reason);