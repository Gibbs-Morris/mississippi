using System;

using Mississippi.Ripples.Abstractions.Actions;
using Mississippi.Ripples.Abstractions.State;


namespace Mississippi.Ripples.Abstractions;

/// <summary>
///     Central state container for the Ripples state management system.
///     Manages both local feature states and server-synced projection states.
/// </summary>
/// <remarks>
///     <para>
///         The store follows a Redux-like pattern with actions, reducers, middleware, and effects.
///         Actions are dispatched to update state synchronously via reducers, and asynchronously
///         via effects for server communication.
///     </para>
///     <para>
///         State is organized into feature slices (local UI state) and projection states
///         (server-synced entities). Use <see cref="GetFeatureState{TState}" /> for feature states
///         and <see cref="GetProjectionState{T}" /> for projections.
///     </para>
/// </remarks>
public interface IRippleStore : IDisposable
{
    /// <summary>
    ///     Dispatches an action to the store.
    ///     Actions are processed by middleware, then reducers, then effects.
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
    TState GetFeatureState<TState>()
        where TState : class, IFeatureState;

    /// <summary>
    ///     Gets the current state for a projection entity.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>The current projection state, or null if not tracked.</returns>
    IProjectionState<T>? GetProjectionState<T>(
        string entityId
    )
        where T : class;

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