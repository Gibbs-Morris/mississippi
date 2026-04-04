using System;

using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Event emitted when a saga step fails.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.DomainModeling.Abstractions.SagaStepFailed")]
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGASTEPFAILED", version: 1)]
public sealed record SagaStepFailed
{
    /// <summary>
    ///     Gets the unique attempt identifier that failed.
    /// </summary>
    [Id(4)]
    public required Guid AttemptId { get; init; }

    /// <summary>
    ///     Gets the error code describing the failure.
    /// </summary>
    [Id(2)]
    public required string ErrorCode { get; init; }

    /// <summary>
    ///     Gets the optional error message describing the failure.
    /// </summary>
    [Id(3)]
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Gets the framework-issued operation key for the failed step operation.
    /// </summary>
    [Id(5)]
    public required string OperationKey { get; init; }

    /// <summary>
    ///     Gets the step index that failed.
    /// </summary>
    [Id(0)]
    public required int StepIndex { get; init; }

    /// <summary>
    ///     Gets the step name that failed.
    /// </summary>
    [Id(1)]
    public required string StepName { get; init; }
}