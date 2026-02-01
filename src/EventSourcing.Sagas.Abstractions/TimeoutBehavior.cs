namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Defines the behavior when a saga step times out.
/// </summary>
public enum TimeoutBehavior
{
    /// <summary>
    ///     Treat the timeout as a failure and trigger compensation. This is the default.
    /// </summary>
    FailAndCompensate = 0,

    /// <summary>
    ///     Retry the step with exponential backoff before failing.
    /// </summary>
    RetryWithBackoff = 1,

    /// <summary>
    ///     Pause the saga and await manual intervention.
    /// </summary>
    AwaitIntervention = 2,
}