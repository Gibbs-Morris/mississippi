using System;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Classifies saga lifecycle events used by orchestration and reminder recovery.
/// </summary>
internal static class SagaLifecycleEventClassifier
{
    /// <summary>
    ///     Determines whether the event participates in saga orchestration and requires a durable wake-up reminder.
    /// </summary>
    /// <param name="eventData">The domain event to classify.</param>
    /// <returns><see langword="true" /> when the event drives saga orchestration; otherwise, <see langword="false" />.</returns>
    public static bool IsOrchestrationLifecycleEvent(
        object eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return eventData is SagaStartedEvent or SagaStepCompleted or SagaStepFailed or SagaCompensating
            or SagaStepCompensated;
    }

    /// <summary>
    ///     Determines whether the event is a replay-safe saga lifecycle boundary.
    /// </summary>
    /// <param name="eventData">The domain event to classify.</param>
    /// <returns><see langword="true" /> when replaying the boundary is safe; otherwise, <see langword="false" />.</returns>
    public static bool IsReplayBoundaryEvent(
        object eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return eventData is SagaStartedEvent or SagaStepCompleted or SagaCompensating or SagaStepCompensated;
    }

    /// <summary>
    ///     Determines whether the event is a saga input event emitted immediately after saga start.
    /// </summary>
    /// <param name="eventData">The domain event to classify.</param>
    /// <returns><see langword="true" /> when the event is a generic <see cref="SagaInputProvided{TInput}" />; otherwise, <see langword="false" />.</returns>
    public static bool IsStartInputEvent(
        object eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        Type eventType = eventData.GetType();
        return eventType.IsGenericType && (eventType.GetGenericTypeDefinition() == typeof(SagaInputProvided<>));
    }

    /// <summary>
    ///     Determines whether the event transitions the saga to a terminal state.
    /// </summary>
    /// <param name="eventData">The domain event to classify.</param>
    /// <returns><see langword="true" /> when the saga is terminal after the event; otherwise, <see langword="false" />.</returns>
    public static bool IsTerminalLifecycleEvent(
        object eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return eventData is SagaCompleted or SagaCompensated or SagaFailed;
    }
}