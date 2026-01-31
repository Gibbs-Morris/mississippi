using System.Collections.Generic;

using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Reservoir.Abstractions.Events;

/// <summary>
///     Emitted when store state is restored via a system action.
/// </summary>
/// <remarks>
///     <para>
///         This event fires after <see cref="RestoreStateAction" /> or
///         <see cref="ResetToInitialStateAction" /> is processed. It provides
///         both the previous and new state snapshots for comparison.
///     </para>
///     <para>
///         DevTools uses this event to update its state display after time-travel
///         operations like jump-to-state, reset, or rollback.
///     </para>
/// </remarks>
/// <param name="PreviousSnapshot">The state snapshot before restoration.</param>
/// <param name="NewSnapshot">The state snapshot after restoration.</param>
/// <param name="Cause">The system action that triggered the restoration.</param>
public sealed record StateRestoredEvent(
    IReadOnlyDictionary<string, object> PreviousSnapshot,
    IReadOnlyDictionary<string, object> NewSnapshot,
    ISystemAction Cause
) : StoreEventBase;