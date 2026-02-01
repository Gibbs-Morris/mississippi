using System;
using System.Collections.Generic;

using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Reservoir.Abstractions.Events;

/// <summary>
///     Emitted after an action has been processed by reducers.
/// </summary>
/// <remarks>
///     <para>
///         This event fires after reducers have updated state but before effects run.
///         It includes the action and a snapshot of the current state after reduction.
///     </para>
///     <para>
///         DevTools and other integrations use this event to track action history
///         and state changes over time.
///     </para>
/// </remarks>
/// <param name="Timestamp">The UTC timestamp when this event was created.</param>
/// <param name="Action">The action that was processed.</param>
/// <param name="StateSnapshot">A snapshot of all feature states after the action was processed.</param>
public sealed record ActionDispatchedEvent(
    DateTimeOffset Timestamp,
    IAction Action,
    IReadOnlyDictionary<string, object> StateSnapshot
) : StoreEventBase(Timestamp);