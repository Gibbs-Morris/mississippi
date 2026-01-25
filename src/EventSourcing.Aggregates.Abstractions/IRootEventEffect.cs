using System.Collections.Generic;
using System.Threading;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Root-level event effect dispatcher that composes one or more <see cref="IEventEffect{TAggregate}" /> instances.
/// </summary>
/// <typeparam name="TAggregate">The aggregate state type.</typeparam>
/// <remarks>
///     <para>
///         This interface abstracts the effect dispatch mechanism, allowing the grain to depend on a
///         single composite rather than iterating through individual effects. The implementation
///         pre-indexes effects by event type at construction time for O(1) lookup during dispatch.
///     </para>
///     <para>
///         This pattern mirrors <c>IRootReducer{TProjection}</c> for consistency across the framework.
///     </para>
/// </remarks>
public interface IRootEventEffect<TAggregate>
{
    /// <summary>
    ///     Gets the count of registered effects.
    /// </summary>
    int EffectCount { get; }

    /// <summary>
    ///     Dispatches an event to all matching effects and collects yielded events.
    /// </summary>
    /// <param name="eventData">The event to dispatch.</param>
    /// <param name="currentState">The current aggregate state after the event was applied.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     An async enumerable of additional events yielded by effects.
    ///     The caller is responsible for persisting these events.
    /// </returns>
    IAsyncEnumerable<object> DispatchAsync(
        object eventData,
        TAggregate currentState,
        CancellationToken cancellationToken
    );
}