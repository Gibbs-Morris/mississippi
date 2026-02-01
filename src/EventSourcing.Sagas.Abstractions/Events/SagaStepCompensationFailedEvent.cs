using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Sagas.Abstractions.Events;

/// <summary>
///     Emitted when a saga step's compensation fails.
/// </summary>
/// <param name="StepName">The name of the step whose compensation failed.</param>
/// <param name="StepOrder">The execution order of the step.</param>
/// <param name="ErrorCode">The error code identifying the failure.</param>
/// <param name="ErrorMessage">A message describing the failure.</param>
/// <param name="Timestamp">When the compensation failed.</param>
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGASTEPCOMPENSATIONFAILED")]
public sealed record SagaStepCompensationFailedEvent(
    string StepName,
    int StepOrder,
    string ErrorCode,
    string? ErrorMessage,
    DateTimeOffset Timestamp
);