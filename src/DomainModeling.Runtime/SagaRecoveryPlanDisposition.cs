namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Describes the next recovery action the runtime should take for a saga.
/// </summary>
internal enum SagaRecoveryPlanDisposition
{
    /// <summary>
    ///     No recovery work should be performed.
    /// </summary>
    NoAction,

    /// <summary>
    ///     Resume by executing a saga step.
    /// </summary>
    ExecuteStep,

    /// <summary>
    ///     Resume by finalizing the saga as completed.
    /// </summary>
    CompleteSaga,

    /// <summary>
    ///     Resume by finalizing the saga as compensated.
    /// </summary>
    CompensateSaga,

    /// <summary>
    ///     Recovery is blocked and requires manual intervention.
    /// </summary>
    Blocked,

    /// <summary>
    ///     The persisted workflow metadata no longer matches the current saga definition.
    /// </summary>
    WorkflowMismatch,

    /// <summary>
    ///     The saga is already terminal.
    /// </summary>
    Terminal,
}