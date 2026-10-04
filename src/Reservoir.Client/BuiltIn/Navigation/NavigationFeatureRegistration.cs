using System;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Client.BuiltIn.Navigation.Actions;
using Mississippi.Reservoir.Client.BuiltIn.Navigation.Effects;
using Mississippi.Reservoir.Client.BuiltIn.Navigation.Reducers;
using Mississippi.Reservoir.Client.BuiltIn.Navigation.State;


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
///         Register with <see cref="AddBuiltInNavigation(IReservoirBuilder)" /> after calling
///         <c>AddReservoir</c>.
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
    public static IReservoirBuilder AddBuiltInNavigation(
        this IReservoirBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddFeatureState<NavigationState>(feature => feature
            .AddReducer<LocationChangedAction>(NavigationReducers.OnLocationChanged)
            .AddActionEffect<NavigationEffect>());
        return builder;
    }
}