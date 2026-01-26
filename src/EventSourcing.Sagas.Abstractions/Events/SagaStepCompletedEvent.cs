using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Sagas.Abstractions.Events;

/// <summary>
///     Emitted when a saga step completes successfully.
/// </summary>
/// <param name="StepName">The name of the step.</param>
/// <param name="StepOrder">The execution order of the step.</param>
/// <param name="Timestamp">When the step completed.</param>
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGASTEPCOMPLETED")]
public sealed record SagaStepCompletedEvent(
    string StepName,
    int StepOrder,
    DateTimeOffset Timestamp
);
