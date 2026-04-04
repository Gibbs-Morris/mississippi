using System;

using Mississippi.DomainModeling.Abstractions;

using Orleans;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Internal framework command that applies a previously planned saga recovery action.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.DomainModeling.Runtime.ResumeSagaCommand")]
internal sealed record ResumeSagaCommand
{
    /// <summary>
    ///     Gets the optional access-context fingerprint captured for the current caller.
    /// </summary>
    [Id(8)]
    public string? AccessContextFingerprint { get; init; }

    /// <summary>
    ///     Gets the optional attempt identifier to reuse when replaying an in-flight operation.
    /// </summary>
    [Id(6)]
    public Guid? AttemptId { get; init; }

    /// <summary>
    ///     Gets the blocked reason to persist when recovery must stop for operator intervention.
    /// </summary>
    [Id(5)]
    public string? BlockedReason { get; init; }

    /// <summary>
    ///     Gets the execution direction for the planned action, when applicable.
    /// </summary>
    [Id(2)]
    public SagaExecutionDirection? Direction { get; init; }

    /// <summary>
    ///     Gets the planned disposition selected by the recovery planner.
    /// </summary>
    [Id(1)]
    public required SagaRecoveryPlanDisposition Disposition { get; init; }

    /// <summary>
    ///     Gets the optional operation key to reuse when replaying an in-flight operation.
    /// </summary>
    [Id(7)]
    public string? OperationKey { get; init; }

    /// <summary>
    ///     Gets the source requesting the resume action.
    /// </summary>
    [Id(0)]
    public required SagaResumeSource Source { get; init; }

    /// <summary>
    ///     Gets the target step index, when the action addresses a specific step.
    /// </summary>
    [Id(3)]
    public int? StepIndex { get; init; }

    /// <summary>
    ///     Gets the target step name, when the action addresses a specific step.
    /// </summary>
    [Id(4)]
    public string? StepName { get; init; }
}