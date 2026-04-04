using System;

using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Event emitted when a saga step completes successfully.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.DomainModeling.Abstractions.SagaStepCompleted")]
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGASTEPCOMPLETED", 1)]
public sealed record SagaStepCompleted
{
    /// <summary>
    ///     Gets the unique attempt identifier that completed.
    /// </summary>
    [Id(3)]
    public required Guid AttemptId { get; init; }

    /// <summary>
    ///     Gets the timestamp when the step completed.
    /// </summary>
    [Id(2)]
    public required DateTimeOffset CompletedAt { get; init; }

    /// <summary>
    ///     Gets the framework-issued operation key for the completed step operation.
    /// </summary>
    [Id(4)]
    public required string OperationKey { get; init; }

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