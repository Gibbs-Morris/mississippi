using System;

namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Defines the base state contract for saga orchestration.
/// </summary>
public interface ISagaState
{
    /// <summary>
    ///     Gets the unique identifier for the saga instance.
    /// </summary>
    Guid SagaId { get; }

    /// <summary>
    ///     Gets the current saga phase.
    /// </summary>
    SagaPhase Phase { get; }

    /// <summary>
    ///     Gets the index of the last completed step.
    /// </summary>
    int LastCompletedStepIndex { get; }

    /// <summary>
    ///     Gets the correlation identifier for the saga instance.
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    ///     Gets the timestamp when the saga started.
    /// </summary>
    DateTimeOffset? StartedAt { get; }

    /// <summary>
    ///     Gets the hash representing the ordered saga steps.
    /// </summary>
    string? StepHash { get; }
}

/// <summary>
///     Defines the lifecycle phases of a saga instance.
/// </summary>
public enum SagaPhase
{
    /// <summary>
    ///     The saga has not yet started.
    /// </summary>
    NotStarted,

    /// <summary>
    ///     The saga is running its forward steps.
    /// </summary>
    Running,

    /// <summary>
    ///     The saga is compensating prior steps.
    /// </summary>
    Compensating,

    /// <summary>
    ///     The saga completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    ///     The saga completed compensation.
    /// </summary>
    Compensated,

    /// <summary>
    ///     The saga failed without compensation.
    /// </summary>
    Failed,
}
