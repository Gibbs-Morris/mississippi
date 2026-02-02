using System;


namespace Mississippi.Reservoir.Abstractions.Events;

/// <summary>
///     Base record for all store events emitted through <see cref="IStore.StoreEvents" />.
/// </summary>
/// <remarks>
///     <para>
///         Store events provide an observable stream of what happens inside the store.
///         External integrations (DevTools, logging, analytics) subscribe to these events
///         rather than extending the store via inheritance.
///     </para>
///     <para>
///         Events are emitted synchronously during dispatch. Subscribers should not
///         perform long-running operations in their handlers to avoid blocking dispatch.
///     </para>
/// </remarks>
/// <param name="Timestamp">
///     The UTC timestamp when this event was created. Provided by the store via
///     injected <see cref="TimeProvider" /> to support deterministic testing with
///     <c>FakeTimeProvider</c>.
/// </param>
public abstract record StoreEventBase(DateTimeOffset Timestamp);