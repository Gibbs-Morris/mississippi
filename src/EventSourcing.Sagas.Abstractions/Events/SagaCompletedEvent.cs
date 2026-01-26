using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Sagas.Abstractions.Events;

/// <summary>
///     Emitted when a saga completes all steps successfully.
/// </summary>
/// <param name="Timestamp">When the saga completed.</param>
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGACOMPLETED")]
public sealed record SagaCompletedEvent(
    DateTimeOffset Timestamp
);
