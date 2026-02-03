using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;

namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Event emitted when a saga starts.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Sagas.Abstractions.SagaStartedEvent")]
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGASTARTED")]
public sealed record SagaStartedEvent
{
    /// <summary>
    ///     Gets the saga identifier.
    /// </summary>
    [Id(0)]
    public required Guid SagaId { get; init; }

    /// <summary>
    ///     Gets the hash representing the step ordering for the saga.
    /// </summary>
    [Id(1)]
    public required string StepHash { get; init; }

    /// <summary>
    ///     Gets the timestamp when the saga started.
    /// </summary>
    [Id(2)]
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>
    ///     Gets the optional correlation identifier.
    /// </summary>
    [Id(3)]
    public string? CorrelationId { get; init; }
}

/// <summary>
///     Event emitted when a saga step completes successfully.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Sagas.Abstractions.SagaStepCompleted")]
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGASTEPCOMPLETED")]
public sealed record SagaStepCompleted
{
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

    /// <summary>
    ///     Gets the timestamp when the step completed.
    /// </summary>
    [Id(2)]
    public required DateTimeOffset CompletedAt { get; init; }
}

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

/// <summary>
///     Event emitted when a saga begins compensation.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Sagas.Abstractions.SagaCompensating")]
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGACOMPENSATING")]
public sealed record SagaCompensating
{
    /// <summary>
    ///     Gets the step index to start compensating from.
    /// </summary>
    [Id(0)]
    public required int FromStepIndex { get; init; }
}

/// <summary>
///     Event emitted when a saga step is compensated.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Sagas.Abstractions.SagaStepCompensated")]
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGASTEPCOMPENSATED")]
public sealed record SagaStepCompensated
{
    /// <summary>
    ///     Gets the step index that was compensated.
    /// </summary>
    [Id(0)]
    public required int StepIndex { get; init; }

    /// <summary>
    ///     Gets the step name that was compensated.
    /// </summary>
    [Id(1)]
    public required string StepName { get; init; }
}

/// <summary>
///     Event emitted when a saga completes successfully.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Sagas.Abstractions.SagaCompleted")]
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGACOMPLETED")]
public sealed record SagaCompleted
{
    /// <summary>
    ///     Gets the timestamp when the saga completed.
    /// </summary>
    [Id(0)]
    public required DateTimeOffset CompletedAt { get; init; }
}

/// <summary>
///     Event emitted when a saga completes compensation.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Sagas.Abstractions.SagaCompensated")]
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGACOMPENSATED")]
public sealed record SagaCompensated
{
    /// <summary>
    ///     Gets the timestamp when compensation completed.
    /// </summary>
    [Id(0)]
    public required DateTimeOffset CompletedAt { get; init; }
}

/// <summary>
///     Event emitted when a saga fails.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Sagas.Abstractions.SagaFailed")]
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGAFAILED")]
public sealed record SagaFailed
{
    /// <summary>
    ///     Gets the error code describing the failure.
    /// </summary>
    [Id(0)]
    public required string ErrorCode { get; init; }

    /// <summary>
    ///     Gets the optional error message describing the failure.
    /// </summary>
    [Id(1)]
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Gets the timestamp when the saga failed.
    /// </summary>
    [Id(2)]
    public required DateTimeOffset FailedAt { get; init; }
}
