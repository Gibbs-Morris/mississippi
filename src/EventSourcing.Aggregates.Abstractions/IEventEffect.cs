using System.Collections.Generic;
using System.Threading;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Handles side effects triggered by domain events within aggregate grains.
/// </summary>
/// <remarks>
///     <para>
///         Event effects run synchronously within the grain context after events are
///         persisted. They block the grain until complete, ensuring effects finish
///         before the next command is processed.
///     </para>
///     <para>
///         Effects can yield additional events via <see cref="IAsyncEnumerable{T}" />,
///         enabling streaming scenarios (e.g., LLM token streaming, progressive data fetch).
///         Yielded events are persisted immediately, allowing projections to update in real-time.
///     </para>
///     <para>
///         <b>Performance guidance:</b> Effects should complete quickly (sub-second typical).
///         A warning is logged if an effect takes longer than 1 second. For long-running
///         background work, consider Orleans reminders or external workflow systems.
///     </para>
/// </remarks>
/// <typeparam name="TAggregate">The aggregate state type this effect is registered for.</typeparam>
public interface IEventEffect<TAggregate>
{
    /// <summary>
    ///     Determines whether this effect can handle the given event.
    /// </summary>
    /// <param name="eventData">The event to check.</param>
    /// <returns><c>true</c> if this effect can handle the event; otherwise, <c>false</c>.</returns>
    bool CanHandle(
        object eventData
    );

    /// <summary>
    ///     Handles the event and optionally yields additional events.
    /// </summary>
    /// <param name="eventData">The event that was persisted.</param>
    /// <param name="currentState">The current aggregate state after the event was applied.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     An async enumerable of additional events to persist.
    ///     Yielded events are persisted immediately, enabling real-time projection updates.
    /// </returns>
    IAsyncEnumerable<object> HandleAsync(
        object eventData,
        TAggregate currentState,
        CancellationToken cancellationToken
    );
}