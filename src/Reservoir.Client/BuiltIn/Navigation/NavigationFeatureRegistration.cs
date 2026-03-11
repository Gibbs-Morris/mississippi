#pragma warning disable S1133 // Intentional staged deprecation pending issue #237.
using System;

using Microsoft.Extensions.DependencyInjection;

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
///         Register with <see cref="AddBuiltInNavigation" /> after calling <c>AddReservoir</c>.
///     </para>
///     <para>
///         <strong>Important:</strong> You must also render the
///         <see cref="Components.ReservoirNavigationProvider" /> component in your app
///         to receive <see cref="LocationChangedAction" /> notifications.
///     </para>
/// </remarks>
[Obsolete(
    "Legacy client composition entrypoint. Will be removed once GitHub issue #237 (Host/Sub-Builder Composition Model) is fully implemented. Migrate to ClientBuilder via UseMississippi() once available (see issue #237, in progress). See: https://github.com/Gibbs-Morris/mississippi/issues/237",
    false)]
public static class NavigationFeatureRegistration
{
    /// <summary>
    ///     Adds the built-in navigation feature to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     This registers:
    ///     <list type="bullet">
    ///         <item><see cref="NavigationState" /> as a feature state</item>
    ///         <item>Reducer for <see cref="LocationChangedAction" /></item>
    ///         <item><see cref="NavigationEffect" /> for handling navigation actions</item>
    ///     </list>
    /// </remarks>
    [Obsolete(
        "Legacy client composition entrypoint. Will be removed once GitHub issue #237 (Host/Sub-Builder Composition Model) is fully implemented. Migrate to ClientBuilder via UseMississippi() once available (see issue #237, in progress). See: https://github.com/Gibbs-Morris/mississippi/issues/237",
        false)]
    public static IServiceCollection AddBuiltInNavigation(
        this IServiceCollection services
    )
    {
        // Register the reducer for LocationChangedAction
        services.AddReducer<LocationChangedAction, NavigationState>(NavigationReducers.OnLocationChanged);

        // Register the navigation effect
        services.AddActionEffect<NavigationState, NavigationEffect>();
        return services;
    }
}

#pragma warning restore S1133