using System.Collections.Generic;


namespace Mississippi.Reservoir.Abstractions.Actions;

/// <summary>
///     System action that restores store state from a snapshot.
/// </summary>
/// <remarks>
///     <para>
///         Used by DevTools and other integrations to implement time-travel debugging.
///         When dispatched, the store replaces all feature states with the provided snapshot
///         and emits a <see cref="Events.StateRestoredEvent" />.
///     </para>
///     <para>
///         Feature keys in the snapshot that don't exist in the current store are ignored.
///         Feature states with incompatible types are also ignored.
///     </para>
/// </remarks>
/// <param name="Snapshot">The feature state snapshot to restore, keyed by feature key.</param>
/// <param name="NotifyListeners">Whether to notify subscribers after restoration. Defaults to true.</param>
public sealed record RestoreStateAction(
    IReadOnlyDictionary<string, object> Snapshot,
    bool NotifyListeners = true
) : ISystemAction;
