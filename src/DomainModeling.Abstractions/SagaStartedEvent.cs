using System;

using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Event emitted when a saga starts.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.DomainModeling.Abstractions.SagaStartedEvent")]
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGASTARTED")]
public sealed record SagaStartedEvent
{
    /// <summary>
    ///     Gets the optional access-context fingerprint captured when the saga started.
    /// </summary>
    [Id(6)]
    public string? AccessContextFingerprint { get; init; }

    /// <summary>
    ///     Gets the optional correlation identifier.
    /// </summary>
    [Id(3)]
    public string? CorrelationId { get; init; }

    /// <summary>
    ///     Gets the saga-level recovery mode.
    /// </summary>
    [Id(4)]
    public required SagaRecoveryMode RecoveryMode { get; init; }

    /// <summary>
    ///     Gets the optional saga-level recovery profile.
    /// </summary>
    [Id(5)]
    public string? RecoveryProfile { get; init; }

    /// <summary>
    ///     Gets the saga identifier.
    /// </summary>
    [Id(0)]
    public required Guid SagaId { get; init; }

    /// <summary>
    ///     Gets the timestamp when the saga started.
    /// </summary>
    [Id(2)]
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>
    ///     Gets the hash representing the step ordering for the saga.
    /// </summary>
    [Id(1)]
    public required string StepHash { get; init; }
}