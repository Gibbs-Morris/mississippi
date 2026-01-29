using System;

using Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.Actions;
using Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.State;


namespace Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.Reducers;

/// <summary>
///     Pure reducer functions for the lifecycle feature state.
/// </summary>
/// <remarks>
///     <para>
///         These reducers update <see cref="LifecycleState" /> in response to lifecycle actions.
///         They are pure functions with no side effects.
///     </para>
///     <para>
///         Timestamps are supplied by the action payload to keep reducers pure.
///     </para>
/// </remarks>
public static class LifecycleReducers
{
    /// <summary>
    ///     Updates lifecycle state when app initialization begins.
    /// </summary>
    /// <param name="state">The current lifecycle state.</param>
    /// <param name="action">The app init action.</param>
    /// <returns>A new state with phase set to Initializing.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="state" /> is null.
    /// </exception>
    public static LifecycleState OnAppInit(
        LifecycleState state,
        AppInitAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            Phase = LifecyclePhase.Initializing,
            InitializedAt = action.InitializedAt,
        };
    }

    /// <summary>
    ///     Updates lifecycle state when app becomes ready.
    /// </summary>
    /// <param name="state">The current lifecycle state.</param>
    /// <param name="action">The app ready action.</param>
    /// <returns>A new state with phase set to Ready.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="state" /> is null.
    /// </exception>
    public static LifecycleState OnAppReady(
        LifecycleState state,
        AppReadyAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            Phase = LifecyclePhase.Ready,
            ReadyAt = action.ReadyAt,
        };
    }
}