using System;

using Orleans;

namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Command that starts a saga with the supplied input.
/// </summary>
/// <typeparam name="TInput">The input payload type.</typeparam>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Sagas.Abstractions.StartSagaCommand`1")]
public sealed record StartSagaCommand<TInput>
{
    /// <summary>
    ///     Gets the saga identifier.
    /// </summary>
    [Id(0)]
    public required Guid SagaId { get; init; }

    /// <summary>
    ///     Gets the input payload for the saga.
    /// </summary>
    [Id(1)]
    public required TInput Input { get; init; }

    /// <summary>
    ///     Gets the optional correlation identifier.
    /// </summary>
    [Id(2)]
    public string? CorrelationId { get; init; }
}
