using System;


namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Interface that saga states can optionally implement to provide
///     step tracking information to the saga infrastructure.
/// </summary>
/// <remarks>
///     <para>
///         Saga states that implement this interface allow the infrastructure
///         to track which step is currently being executed, maintain saga identity,
///         and detect step definition drift. States that do not implement this
///         interface will have step tracking derived from the
///         <see cref="Projections.SagaStatusProjection" />.
///     </para>
///     <para>
///         Implementing this interface enables the saga effects to properly propagate
///         saga identity and correlation IDs to steps and compensations, which is
///         critical for observability, idempotency, and retry logic.
///     </para>
/// </remarks>
public interface ISagaState
{
    /// <summary>
    ///     Gets the correlation identifier linking related operations.
    /// </summary>
    /// <remarks>
    ///     Set when the saga is started via <see cref="Commands.StartSagaCommand{TInput}" />.
    /// </remarks>
    string? CorrelationId { get; }

    /// <summary>
    ///     Gets the current attempt number for the active step (1-based).
    /// </summary>
    /// <remarks>
    ///     Incremented when a step is retried via <see cref="CompensationStrategy.RetryThenCompensate" />.
    ///     Reset to 1 when moving to a new step.
    /// </remarks>
    int CurrentStepAttempt { get; }

    /// <summary>
    ///     Gets the index of the last completed step (0-based).
    ///     Returns -1 if no steps have completed.
    /// </summary>
    int LastCompletedStepIndex { get; }

    /// <summary>
    ///     Gets the current phase of the saga.
    /// </summary>
    SagaPhase Phase { get; }

    /// <summary>
    ///     Gets the unique identifier for this saga instance.
    /// </summary>
    /// <remarks>
    ///     Set when the saga is started. Typically matches the aggregate key.
    /// </remarks>
    Guid SagaId { get; }

    /// <summary>
    ///     Gets the timestamp when the saga was started.
    /// </summary>
    DateTimeOffset? StartedAt { get; }

    /// <summary>
    ///     Gets the step definition hash recorded when the saga started.
    /// </summary>
    /// <remarks>
    ///     Used to detect step definition drift. If the current registry hash
    ///     differs from this value, the saga may be incompatible with the
    ///     registered step definitions.
    /// </remarks>
    string? StepHash { get; }

    /// <summary>
    ///     Creates a copy with the saga phase updated.
    /// </summary>
    /// <param name="phase">The new saga phase.</param>
    /// <returns>A new instance with the phase applied.</returns>
    ISagaState ApplyPhase(
        SagaPhase phase
    );

    /// <summary>
    ///     Creates a copy with saga identity fields set when the saga is started.
    /// </summary>
    /// <param name="sagaId">The saga instance identifier.</param>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="stepHash">The step definition hash.</param>
    /// <param name="startedAt">The saga start timestamp.</param>
    /// <returns>A new instance with the saga started fields applied.</returns>
    /// <remarks>
    ///     Implementers should use record <c>with</c> expressions:
    ///     <code>
    ///     return this with
    ///     {
    ///         SagaId = sagaId,
    ///         CorrelationId = correlationId,
    ///         StepHash = stepHash,
    ///         StartedAt = startedAt,
    ///         Phase = SagaPhase.Running,
    ///         LastCompletedStepIndex = -1,
    ///         CurrentStepAttempt = 1
    ///     };
    ///     </code>
    /// </remarks>
    ISagaState ApplySagaStarted(
        Guid sagaId,
        string? correlationId,
        string? stepHash,
        DateTimeOffset startedAt
    );

    /// <summary>
    ///     Creates a copy with updated step progress tracking.
    /// </summary>
    /// <param name="lastCompletedStepIndex">The index of the last completed step.</param>
    /// <param name="currentStepAttempt">The current step attempt number.</param>
    /// <returns>A new instance with the step progress applied.</returns>
    ISagaState ApplyStepProgress(
        int lastCompletedStepIndex,
        int currentStepAttempt
    );
}