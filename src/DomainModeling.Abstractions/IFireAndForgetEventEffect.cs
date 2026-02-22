using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Handles fire-and-forget side effects triggered by domain events.
/// </summary>
/// <remarks>
///     <para>
///         Unlike <see cref="IEventEffect{TAggregate}" />, fire-and-forget effects run in a
///         separate grain context and do not block the aggregate. The aggregate dispatches
///         the effect and continues immediately without waiting for completion.
///     </para>
///     <para>
///         Fire-and-forget effects CANNOT yield additional events. If the effect needs to
///         trigger further state changes, it should send commands through the normal
///         aggregate command API.
///     </para>
///     <para>
///         Effects are resolved via dependency injection and can inject any required services
///         (HTTP clients, other grain factories, message publishers, etc.). The worker grain
///         is just runtime infrastructure—the effect class itself is where all business logic
///         lives and should be easily unit-testable.
///     </para>
/// </remarks>
/// <typeparam name="TEvent">The event type this effect handles.</typeparam>
/// <typeparam name="TAggregate">The aggregate state type this effect is registered for.</typeparam>
public interface IFireAndForgetEventEffect<in TEvent, in TAggregate>
    where TEvent : class
    where TAggregate : class
{
    /// <summary>
    ///     Handles the event asynchronously in a fire-and-forget context.
    /// </summary>
    /// <param name="eventData">The event that was persisted.</param>
    /// <param name="aggregateState">A snapshot of the aggregate state after the event was applied.</param>
    /// <param name="brookKey">The brook key identifying the aggregate instance.</param>
    /// <param name="eventPosition">The position of the event in the brook.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleAsync(
        TEvent eventData,
        TAggregate aggregateState,
        string brookKey,
        long eventPosition,
        CancellationToken cancellationToken
    );
}