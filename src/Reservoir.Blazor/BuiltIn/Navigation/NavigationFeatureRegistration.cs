using System;

using Mississippi.Reservoir.Abstractions.Builders;
using Mississippi.Reservoir.Blazor.BuiltIn.Navigation.Actions;
using Mississippi.Reservoir.Blazor.BuiltIn.Navigation.Effects;
using Mississippi.Reservoir.Blazor.BuiltIn.Navigation.Reducers;
using Mississippi.Reservoir.Blazor.BuiltIn.Navigation.State;


namespace Mississippi.Reservoir.Blazor.BuiltIn.Navigation;

/// <summary>
///     Extension methods for registering the built-in navigation feature.
/// </summary>
/// <remarks>
///     <para>
///         The navigation feature provides Redux-style navigation state management for Blazor.
///         It tracks the current URI, previous URI, and navigation count in the store.
///     </para>
///     <para>
    ///         Register with <see cref="AddBuiltInNavigation" /> after calling <c>AddReservoir</c> on the builder.
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
    ///     Adds the built-in navigation feature to the Reservoir builder.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
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
        this IReservoirBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Register the reducer for LocationChangedAction and the navigation effect.
        builder.AddFeature<NavigationState>()
            .AddReducer<LocationChangedAction>(NavigationReducers.OnLocationChanged)
            .AddActionEffect<NavigationEffect>()
            .Done();
        return builder;
    }
}