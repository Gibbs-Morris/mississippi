namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Defines the compensation strategy when a saga step fails.
/// </summary>
public enum CompensationStrategy
{
    /// <summary>
    ///     First step failure triggers immediate compensation of all completed steps in reverse order.
    ///     This is the safest default behavior.
    /// </summary>
    Immediate = 0,

    /// <summary>
    ///     Retry the failed step up to the configured maximum attempts before triggering compensation.
    /// </summary>
    RetryThenCompensate = 1,

    /// <summary>
    ///     Only compensate when explicitly requested via a cancel command.
    ///     Useful for manual intervention workflows.
    /// </summary>
    Manual = 2,
}