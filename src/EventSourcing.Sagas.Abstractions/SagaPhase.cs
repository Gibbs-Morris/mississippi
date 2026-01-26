namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Represents the current phase of a saga's lifecycle.
/// </summary>
public enum SagaPhase
{
    /// <summary>
    ///     The saga has not started.
    /// </summary>
    NotStarted = 0,

    /// <summary>
    ///     The saga is actively executing steps.
    /// </summary>
    Running = 1,

    /// <summary>
    ///     The saga is waiting for a step's verification condition to be met.
    /// </summary>
    AwaitingVerification = 2,

    /// <summary>
    ///     The saga is compensating completed steps due to a failure.
    /// </summary>
    Compensating = 3,

    /// <summary>
    ///     The saga completed successfully.
    /// </summary>
    Completed = 4,

    /// <summary>
    ///     The saga failed and could not be compensated.
    /// </summary>
    Failed = 5,
}
