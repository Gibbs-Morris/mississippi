using System;

using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Event emitted when a saga step execution attempt starts.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.DomainModeling.Abstractions.SagaStepExecutionStarted")]
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGASTEPEXECUTIONSTARTED")]
public sealed record SagaStepExecutionStarted
{
    /// <summary>
    ///     Gets the optional access-context fingerprint associated with the explicit resume attempt.
    /// </summary>
    [Id(7)]
    public string? AccessContextFingerprint { get; init; }

    /// <summary>
    ///     Gets the unique attempt identifier.
    /// </summary>
    [Id(3)]
    public required Guid AttemptId { get; init; }

    /// <summary>
    ///     Gets the execution direction for the attempt.
    /// </summary>
    [Id(2)]
    public required SagaExecutionDirection Direction { get; init; }

    /// <summary>
    ///     Gets the framework-issued operation key for the step operation.
    /// </summary>
    [Id(4)]
    public required string OperationKey { get; init; }

    /// <summary>
    ///     Gets the source that triggered the execution attempt.
    /// </summary>
    [Id(5)]
    public required SagaResumeSource Source { get; init; }

    /// <summary>
    ///     Gets the timestamp when the attempt started.
    /// </summary>
    [Id(6)]
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>
    ///     Gets the step index being executed.
    /// </summary>
    [Id(0)]
    public required int StepIndex { get; init; }

    /// <summary>
    ///     Gets the step name being executed.
    /// </summary>
    [Id(1)]
    public required string StepName { get; init; }
}