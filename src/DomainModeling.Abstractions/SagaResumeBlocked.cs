using System;

using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Event emitted when a saga resume attempt is blocked and requires operator intervention.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.DomainModeling.Abstractions.SagaResumeBlocked")]
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGARESUMEBLOCKED", 1)]
public sealed record SagaResumeBlocked
{
    /// <summary>
    ///     Gets the optional access-context fingerprint associated with the explicit resume attempt.
    /// </summary>
    [Id(6)]
    public string? AccessContextFingerprint { get; init; }

    /// <summary>
    ///     Gets the timestamp when the resume attempt was blocked.
    /// </summary>
    [Id(5)]
    public required DateTimeOffset BlockedAt { get; init; }

    /// <summary>
    ///     Gets the operator-visible reason the resume attempt was blocked.
    /// </summary>
    [Id(2)]
    public required string BlockedReason { get; init; }

    /// <summary>
    ///     Gets the execution direction that remains blocked.
    /// </summary>
    [Id(3)]
    public required SagaExecutionDirection Direction { get; init; }

    /// <summary>
    ///     Gets the source that attempted the blocked resume.
    /// </summary>
    [Id(4)]
    public required SagaResumeSource Source { get; init; }

    /// <summary>
    ///     Gets the step index that remains blocked.
    /// </summary>
    [Id(0)]
    public required int StepIndex { get; init; }

    /// <summary>
    ///     Gets the step name that remains blocked.
    /// </summary>
    [Id(1)]
    public required string StepName { get; init; }
}