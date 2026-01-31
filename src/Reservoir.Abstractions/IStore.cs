using System;
using System.Collections.Generic;

using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.Events;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Abstractions;

/// <summary>
///     Central state container for the Reservoir state management system.
///     Manages local feature states using a Redux-like pattern.
/// </summary>
/// <remarks>
///     <para>
///         The store follows a Redux-like pattern with actions, action reducers, middleware, and effects.
///         Actions are dispatched to update state synchronously via action reducers, and asynchronously
///         via effects for side-effect operations.
///     </para>
///     <para>
///         State is organized into feature slices. Use <see cref="GetState{TState}" /> to access
///         feature states. Subscribe to changes via <see cref="Subscribe" />.
///     </para>
///     <para>
///         External integrations can observe store activity via <see cref="StoreEvents" /> without
///         requiring inheritance. System actions (<see cref="ISystemAction" />) allow external
///         components to command the store through the standard dispatch mechanism.
///     </para>
/// </remarks>
public interface IStore : IDisposable
{
    /// <summary>
    ///     Gets an observable stream of store events for external integrations.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Subscribers receive events for action dispatch, state restoration, and initialization.
    ///         This enables DevTools, logging, analytics, and other integrations to observe store
    ///         behavior without subclassing.
    ///     </para>
    ///     <para>
    ///         Events are emitted synchronously during dispatch. Subscribers should avoid
    ///         long-running operations in their handlers to prevent blocking the dispatch pipeline.
    ///     </para>
    /// </remarks>
    IObservable<StoreEventBase> StoreEvents { get; }

    /// <summary>
    ///     Gets a snapshot of all current feature states.
    /// </summary>
    /// <returns>A dictionary of feature states keyed by feature key.</returns>
    IReadOnlyDictionary<string, object> GetStateSnapshot();

    /// <summary>
    ///     Dispatches an action to the store.
    ///     Actions are processed by middleware, then action reducers, then effects.
    /// </summary>
    /// <param name="action">The action to dispatch.</param>
    void Dispatch(
        IAction action
    );

    /// <summary>
    ///     Gets the current state for a feature slice.
    /// </summary>
    /// <typeparam name="TState">The feature state type (must implement <see cref="IFeatureState" />).</typeparam>
    /// <returns>The current feature state.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when no action reducer is registered for the feature state.
    /// </exception>
    TState GetState<TState>()
        where TState : class, IFeatureState;

    /// <summary>
    ///     Subscribes to state changes.
    ///     The listener is invoked after any action is processed.
    /// </summary>
    /// <param name="listener">The listener to invoke on changes.</param>
    /// <returns>A disposable to unsubscribe.</returns>
    IDisposable Subscribe(
        Action listener
    );
}