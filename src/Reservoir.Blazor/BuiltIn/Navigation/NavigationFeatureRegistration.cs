using Microsoft.Extensions.DependencyInjection;

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
///         <strong>Usage:</strong>
///     </para>
///     <code>
///         services.AddReservoir();
///         services.AddBuiltInNavigation();
///     </code>
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