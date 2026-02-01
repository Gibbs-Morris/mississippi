using System;

using Orleans;


namespace Mississippi.EventSourcing.Sagas.Abstractions.Commands;

/// <summary>
///     Command to start a saga with the specified input data.
/// </summary>
/// <typeparam name="TInput">The type of input data for the saga.</typeparam>
/// <param name="SagaId">The unique identifier for this saga instance.</param>
/// <param name="Input">The input data required to start the saga.</param>
/// <param name="CorrelationId">Optional correlation identifier for tracking.</param>
/// <remarks>
///     This command is handled by the saga's command handler which emits
///     <see cref="Events.SagaStartedEvent" /> and <see cref="Events.SagaStepStartedEvent" />
///     to kick off the saga workflow.
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Sagas.Abstractions.Commands.StartSagaCommand`1")]
public sealed record StartSagaCommand<TInput>(
    [property: Id(0)] Guid SagaId,
    [property: Id(1)] TInput Input,
    [property: Id(2)] string? CorrelationId = null
)
    where TInput : class;