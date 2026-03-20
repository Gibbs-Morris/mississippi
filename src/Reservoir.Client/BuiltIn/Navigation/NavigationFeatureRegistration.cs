using System;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Client.BuiltIn.Navigation.Actions;
using Mississippi.Reservoir.Client.BuiltIn.Navigation.Effects;
using Mississippi.Reservoir.Client.BuiltIn.Navigation.Reducers;
using Mississippi.Reservoir.Client.BuiltIn.Navigation.State;
using Mississippi.Reservoir.Core;


namespace Mississippi.Reservoir.Client.BuiltIn.Navigation;

/// <summary>
///     Extension methods for registering the built-in navigation feature.
/// </summary>
/// <remarks>
///     <para>
///         The navigation feature provides Redux-style navigation state management for Blazor.
///         It tracks the current URI, previous URI, and navigation count in the store.
///     </para>
///     <para>
///         <strong>Important:</strong> You must also render the
///         <see cref="Components.ReservoirNavigationProvider" /> component in your app
///         to receive <see cref="LocationChangedAction" /> notifications.
///     </para>
/// </remarks>
public static class NavigationFeatureRegistration
{
    /// <summary>
    ///     Adds the built-in navigation feature to the service collection.
    /// </summary>
    /// <param name="reservoir">The Reservoir builder.</param>
    /// <returns>The Reservoir builder for chaining.</returns>
    /// <remarks>
    ///     This registers:
    ///     <list type="bullet">
    ///         <item><see cref="NavigationState" /> as a feature state</item>
    ///         <item>Reducer for <see cref="LocationChangedAction" /></item>
    ///         <item><see cref="NavigationEffect" /> for handling navigation actions</item>
    ///     </list>
    /// </remarks>
    public static IReservoirBuilder AddBuiltInNavigation(
        this IReservoirBuilder reservoir
    )
    {
        ArgumentNullException.ThrowIfNull(reservoir);
        reservoir.AddFeature<NavigationState>(feature => feature
            .AddReducer<NavigationState, LocationChangedAction>(NavigationReducers.OnLocationChanged)
            .AddActionEffect<NavigationState, NavigationEffect>());
        return reservoir;
    }
}