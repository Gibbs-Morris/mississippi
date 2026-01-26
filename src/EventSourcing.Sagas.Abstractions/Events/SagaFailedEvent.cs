using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Sagas.Abstractions.Events;

/// <summary>
///     Emitted when a saga fails and cannot be compensated.
/// </summary>
/// <param name="Reason">The reason for the final failure.</param>
/// <param name="Timestamp">When the saga failed.</param>
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGAFAILED")]
public sealed record SagaFailedEvent(
    string Reason,
    DateTimeOffset Timestamp
);
