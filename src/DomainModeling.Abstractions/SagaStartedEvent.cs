using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Event emitted when a saga starts.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Sagas.Abstractions.SagaStartedEvent")]
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGASTARTED")]
public sealed record SagaStartedEvent
{
    /// <summary>
    ///     Gets the optional correlation identifier.
    /// </summary>
    [Id(3)]
    public string? CorrelationId { get; init; }

    /// <summary>
    ///     Gets the saga identifier.
    /// </summary>
    [Id(0)]
    public required Guid SagaId { get; init; }

    /// <summary>
    ///     Gets the timestamp when the saga started.
    /// </summary>
    [Id(2)]
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>
    ///     Gets the hash representing the step ordering for the saga.
    /// </summary>
    [Id(1)]
    public required string StepHash { get; init; }
}