using System;


namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Describes a saga step execution attempt.
/// </summary>
public sealed record SagaStepExecutionContext
{
    /// <summary>
    ///     Gets the unique attempt identifier.
    /// </summary>
    public required Guid AttemptId { get; init; }

    /// <summary>
    ///     Gets the timestamp when the attempt started.
    /// </summary>
    public required DateTimeOffset AttemptStartedAt { get; init; }

    /// <summary>
    ///     Gets the saga execution direction.
    /// </summary>
    public required SagaExecutionDirection Direction { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the attempt is replaying previously started work.
    /// </summary>
    public required bool IsReplay { get; init; }

    /// <summary>
    ///     Gets the framework-issued operation key for the attempt.
    /// </summary>
    public required string OperationKey { get; init; }

    /// <summary>
    ///     Gets the saga identifier.
    /// </summary>
    public required Guid SagaId { get; init; }

    /// <summary>
    ///     Gets the source that triggered the execution attempt.
    /// </summary>
    public required SagaResumeSource Source { get; init; }

    /// <summary>
    ///     Gets the zero-based step index.
    /// </summary>
    public required int StepIndex { get; init; }

    /// <summary>
    ///     Gets the step name.
    /// </summary>
    public required string StepName { get; init; }
}
