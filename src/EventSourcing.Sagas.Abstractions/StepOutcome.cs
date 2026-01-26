namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Represents the outcome of a saga step execution.
/// </summary>
public enum StepOutcome
{
    /// <summary>
    ///     The step has started execution.
    /// </summary>
    Started = 0,

    /// <summary>
    ///     The step completed successfully.
    /// </summary>
    Succeeded = 1,

    /// <summary>
    ///     The step failed during execution.
    /// </summary>
    Failed = 2,

    /// <summary>
    ///     The step was compensated (rolled back).
    /// </summary>
    Compensated = 3,

    /// <summary>
    ///     The step timed out before completing.
    /// </summary>
    TimedOut = 4,
}
