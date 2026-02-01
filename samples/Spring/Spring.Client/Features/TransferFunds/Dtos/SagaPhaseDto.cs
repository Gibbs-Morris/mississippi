namespace Spring.Client.Features.TransferFunds.Dtos;

/// <summary>
///     Client-side DTO for <see cref="Mississippi.EventSourcing.Sagas.Abstractions.SagaPhase" />.
/// </summary>
public enum SagaPhaseDto
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
    ///     The saga failed and cannot be compensated.
    /// </summary>
    Failed = 5,
}