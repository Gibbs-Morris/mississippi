using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Represents the next recovery action selected for a saga.
/// </summary>
internal sealed record SagaRecoveryPlan
{
    /// <summary>
    ///     Gets the recovery action that should be performed.
    /// </summary>
    public required SagaRecoveryPlanDisposition Disposition { get; init; }

    /// <summary>
    ///     Gets the direction associated with the selected recovery action, when applicable.
    /// </summary>
    public SagaExecutionDirection? Direction { get; init; }

    /// <summary>
    ///     Gets the operator-visible reason for a blocked or mismatched recovery plan.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    ///     Gets the step metadata selected for execution, when the plan resumes a step.
    /// </summary>
    public SagaStepInfo? Step { get; init; }
}