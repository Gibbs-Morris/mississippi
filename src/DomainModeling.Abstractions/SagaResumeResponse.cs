using System;


namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Describes the outcome of a manual saga resume request.
/// </summary>
public sealed record SagaResumeResponse
{
    /// <summary>
    ///     Gets the operator-visible blocked reason when the resume request could not proceed automatically.
    /// </summary>
    public string? BlockedReason { get; init; }

    /// <summary>
    ///     Gets the request disposition.
    /// </summary>
    public required SagaResumeRequestDisposition Disposition { get; init; }

    /// <summary>
    ///     Gets an optional explanatory message for the request outcome.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    ///     Gets the pending step index, when the response relates to a specific step.
    /// </summary>
    public int? PendingStepIndex { get; init; }

    /// <summary>
    ///     Gets the pending step name, when the response relates to a specific step.
    /// </summary>
    public string? PendingStepName { get; init; }

    /// <summary>
    ///     Gets the timestamp when the request was processed.
    /// </summary>
    public required DateTimeOffset ProcessedAt { get; init; }

    /// <summary>
    ///     Gets the saga identifier.
    /// </summary>
    public required Guid SagaId { get; init; }

    /// <summary>
    ///     Gets the source that triggered the request outcome.
    /// </summary>
    public required SagaResumeSource Source { get; init; }
}

/// <summary>
///     Describes the outcome of a manual saga resume request.
/// </summary>
public enum SagaResumeRequestDisposition
{
    /// <summary>
    ///     The manual resume request was accepted and recovery work was scheduled or completed.
    /// </summary>
    Accepted,

    /// <summary>
    ///     The request was rejected because the saga is currently blocked.
    /// </summary>
    Blocked,

    /// <summary>
    ///     The saga is already terminal and cannot be resumed.
    /// </summary>
    Terminal,

    /// <summary>
    ///     The request cannot proceed because the persisted workflow identity no longer matches the current saga definition.
    /// </summary>
    WorkflowMismatch,

    /// <summary>
    ///     The request completed without taking any recovery action.
    /// </summary>
    NoAction,
}