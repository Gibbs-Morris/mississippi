using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Sagas.Abstractions.Events;

/// <summary>
///     Emitted when a saga begins compensation.
/// </summary>
/// <param name="FromStep">The step that triggered compensation.</param>
/// <param name="Timestamp">When compensation started.</param>
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGACOMPENSATING")]
public sealed record SagaCompensatingEvent(
    string FromStep,
    DateTimeOffset Timestamp
);
