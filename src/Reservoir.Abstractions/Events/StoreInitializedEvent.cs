using System;
using System.Collections.Generic;


namespace Mississippi.Reservoir.Abstractions.Events;

/// <summary>
///     Emitted when the store completes initialization.
/// </summary>
/// <remarks>
///     <para>
///         This event fires once during store construction after all feature states
///         have been registered. It provides the initial state snapshot.
///     </para>
///     <para>
///         DevTools uses this event to initialize its connection with the current
///         application state.
///     </para>
/// </remarks>
/// <param name="Timestamp">The UTC timestamp when this event was created.</param>
/// <param name="InitialSnapshot">A snapshot of all feature states at initialization.</param>
public sealed record StoreInitializedEvent(
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, object> InitialSnapshot
) : StoreEventBase(Timestamp);