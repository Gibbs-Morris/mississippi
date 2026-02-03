using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Event emitted when a saga step completes successfully.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Sagas.Abstractions.SagaStepCompleted")]
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGASTEPCOMPLETED")]
public sealed record SagaStepCompleted
{
    /// <summary>
    ///     Gets the timestamp when the step completed.
    /// </summary>
    [Id(2)]
    public required DateTimeOffset CompletedAt { get; init; }

    /// <summary>
    ///     Gets the step index that completed.
    /// </summary>
    [Id(0)]
    public required int StepIndex { get; init; }

    /// <summary>
    ///     Gets the step name that completed.
    /// </summary>
    [Id(1)]
    public required string StepName { get; init; }
}