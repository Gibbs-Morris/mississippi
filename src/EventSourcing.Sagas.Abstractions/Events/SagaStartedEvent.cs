using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Sagas.Abstractions.Events;

/// <summary>
///     Emitted when a saga starts execution.
/// </summary>
/// <param name="SagaId">The unique identifier of the saga instance.</param>
/// <param name="SagaType">The type name of the saga state.</param>
/// <param name="StepHash">A hash of the step definitions for versioning.</param>
/// <param name="CorrelationId">Optional correlation identifier for tracking related operations.</param>
/// <param name="Timestamp">When the saga started.</param>
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGASTARTED")]
public sealed record SagaStartedEvent(
    string SagaId,
    string SagaType,
    string StepHash,
    string? CorrelationId,
    DateTimeOffset Timestamp
);