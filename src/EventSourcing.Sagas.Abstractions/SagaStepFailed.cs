using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;

namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Event emitted when a saga step fails.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Sagas.Abstractions.SagaStepFailed")]
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGASTEPFAILED")]
public sealed record SagaStepFailed
{
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
}
