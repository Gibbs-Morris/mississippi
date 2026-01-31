using System;

using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Reservoir.Abstractions.Events;

/// <summary>
///     Emitted before an action is processed by reducers.
/// </summary>
/// <remarks>
///     <para>
///         This event fires after middleware has processed the action but before
///         reducers run. It provides a preview of the action about to be applied.
///     </para>
///     <para>
///         System actions (<see cref="ISystemAction" />) also emit this event before
///         the store handles them internally.
///     </para>
/// </remarks>
/// <param name="Timestamp">The UTC timestamp when this event was created.</param>
/// <param name="Action">The action about to be processed.</param>
public sealed record ActionDispatchingEvent(
    DateTimeOffset Timestamp,
    IAction Action
) : StoreEventBase(Timestamp);