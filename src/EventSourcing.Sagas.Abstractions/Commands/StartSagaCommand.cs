namespace Mississippi.EventSourcing.Sagas.Abstractions.Commands;

/// <summary>
///     Command to start a saga with the specified input data.
/// </summary>
/// <typeparam name="TInput">The type of input data for the saga.</typeparam>
/// <param name="Input">The input data required to start the saga.</param>
/// <param name="CorrelationId">Optional correlation identifier for tracking.</param>
/// <remarks>
///     This command is handled by the saga's command handler which emits
///     <see cref="Events.SagaStartedEvent" /> and <see cref="Events.SagaStepStartedEvent" />
///     to kick off the saga workflow.
/// </remarks>
public sealed record StartSagaCommand<TInput>(TInput Input, string? CorrelationId = null)
    where TInput : class;