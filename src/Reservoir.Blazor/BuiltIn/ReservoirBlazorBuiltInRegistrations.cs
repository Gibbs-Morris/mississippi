using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle;
using Mississippi.Reservoir.Blazor.BuiltIn.Navigation;


namespace Mississippi.Reservoir.Blazor.BuiltIn;

/// <summary>
///     Extension methods for registering all built-in Reservoir Blazor features.
/// </summary>
/// <remarks>
///     <para>
///         This provides a convenient way to register all built-in features at once.
///         You can also register features individually using their specific extension methods.
///     </para>
///     <para>
///         <strong>Usage:</strong>
///     </para>
///     <code>
///         services.AddReservoir();
///         services.AddReservoirBlazorBuiltIns();
///
///         // In App.razor or your root layout:
///         &lt;ReservoirNavigationProvider /&gt;
///     </code>
/// </remarks>
public static class ReservoirBlazorBuiltInRegistrations
{
    /// <summary>
    ///     Adds all built-in Reservoir Blazor features to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     This registers:
    ///     <list type="bullet">
    ///         <item>Navigation feature (state, reducers, effects)</item>
    ///         <item>Lifecycle feature (state, reducers)</item>
    ///     </list>
    ///     <para>
    ///         <strong>Important:</strong> You must also render the
    ///         <see cref="Components.ReservoirNavigationProvider" /> component in your app
    ///         to receive location change notifications.
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddReservoirBlazorBuiltIns(
        this IServiceCollection services
    )
    {
        services.AddBuiltInNavigation();
        services.AddBuiltInLifecycle();
        return services;
    }
}