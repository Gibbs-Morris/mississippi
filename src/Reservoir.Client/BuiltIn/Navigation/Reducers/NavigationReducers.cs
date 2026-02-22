using System;

using Mississippi.Reservoir.Blazor.BuiltIn.Navigation.Actions;
using Mississippi.Reservoir.Blazor.BuiltIn.Navigation.State;


namespace Mississippi.Reservoir.Blazor.BuiltIn.Navigation.Reducers;

/// <summary>
///     Pure reducer functions for the navigation feature state.
/// </summary>
/// <remarks>
///     <para>
///         These reducers update <see cref="NavigationState" /> in response to navigation actions.
///         They are pure functions with no side effects.
///     </para>
///     <para>
///         The navigation effect handles the actual NavigationManager calls;
///         these reducers only update state after navigation has occurred.
///     </para>
/// </remarks>
public static class NavigationReducers
{
    /// <summary>
    ///     Updates navigation state when the location changes.
    /// </summary>
    /// <param name="state">The current navigation state.</param>
    /// <param name="action">The location changed action containing the new URI.</param>
    /// <returns>A new state with updated navigation information.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="state" /> or <paramref name="action" /> is null.</exception>
    public static NavigationState OnLocationChanged(
        NavigationState state,
        LocationChangedAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            PreviousUri = state.CurrentUri,
            CurrentUri = action.Location,
            IsNavigationIntercepted = action.IsNavigationIntercepted,
            NavigationCount = state.NavigationCount + 1,
        };
    }
}