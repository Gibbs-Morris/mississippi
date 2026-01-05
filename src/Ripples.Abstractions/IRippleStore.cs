using System;

using Mississippi.Ripples.Abstractions.Actions;
using Mississippi.Ripples.Abstractions.State;


namespace Mississippi.Ripples.Abstractions;

/// <summary>
///     Central state container for ripple projections.
/// </summary>
public interface IRippleStore : IDisposable
{
    /// <summary>
    ///     Dispatches an action to update state.
    /// </summary>
    /// <param name="action">The action to dispatch.</param>
    void Dispatch(
        IRippleAction action
    );

    /// <summary>
    ///     Gets the current state for a projection entity.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>The current projection state, or null if not tracked.</returns>
    IProjectionState<T>? GetState<T>(
        string entityId
    )
        where T : class;

    /// <summary>
    ///     Subscribes to state changes.
    /// </summary>
    /// <param name="listener">The listener to invoke on changes.</param>
    /// <returns>A disposable to unsubscribe.</returns>
    IDisposable Subscribe(
        Action listener
    );
}