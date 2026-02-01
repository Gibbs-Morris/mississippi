using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Sagas.Abstractions.Events;

/// <summary>
///     Emitted when a saga step is successfully compensated.
/// </summary>
/// <param name="StepName">The name of the compensated step.</param>
/// <param name="StepOrder">The execution order of the compensated step.</param>
/// <param name="Timestamp">When the compensation completed.</param>
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGASTEPCOMPENSATED")]
public sealed record SagaStepCompensatedEvent(string StepName, int StepOrder, DateTimeOffset Timestamp);