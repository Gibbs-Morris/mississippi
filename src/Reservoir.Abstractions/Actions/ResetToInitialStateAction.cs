namespace Mississippi.Reservoir.Abstractions.Actions;

/// <summary>
///     System action that resets the store to its initial state.
/// </summary>
/// <remarks>
///     <para>
///         Used by DevTools and other integrations to reset the application state.
///         When dispatched, the store replaces all feature states with their initial values
///         (as registered during startup) and emits a <see cref="Events.StateRestoredEvent" />.
///     </para>
/// </remarks>
/// <param name="NotifyListeners">Whether to notify subscribers after reset. Defaults to true.</param>
public sealed record ResetToInitialStateAction(bool NotifyListeners = true) : ISystemAction;