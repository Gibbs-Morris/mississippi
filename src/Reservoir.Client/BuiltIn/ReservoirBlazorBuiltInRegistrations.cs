#pragma warning disable S1133 // Intentional staged deprecation pending issue #237.
using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Client.BuiltIn.Lifecycle;
using Mississippi.Reservoir.Client.BuiltIn.Navigation;


namespace Mississippi.Reservoir.Client.BuiltIn;

/// <summary>
///     Extension methods for registering all built-in Reservoir Blazor features.
/// </summary>
/// <remarks>
///     <para>
///         This provides a convenient way to register all built-in features at once.
///         You can also register features individually using their specific extension methods.
///     </para>
///     <para>
///         Register with <see cref="AddReservoirBlazorBuiltIns" /> after calling <c>AddReservoir</c>.
///     </para>
/// </remarks>
[Obsolete(
    "Legacy client composition entrypoint. Will be removed once GitHub issue #237 (Host/Sub-Builder Composition Model) is fully implemented. Migrate to ClientBuilder via UseMississippi() once available (see issue #237, in progress). See: https://github.com/Gibbs-Morris/mississippi/issues/237",
    false)]
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
    [Obsolete(
        "Legacy client composition entrypoint. Will be removed once GitHub issue #237 (Host/Sub-Builder Composition Model) is fully implemented. Migrate to ClientBuilder via UseMississippi() once available (see issue #237, in progress). See: https://github.com/Gibbs-Morris/mississippi/issues/237",
        false)]
    public static IServiceCollection AddReservoirBlazorBuiltIns(
        this IServiceCollection services
    )
    {
        services.AddBuiltInNavigation();
        services.AddBuiltInLifecycle();
        return services;
    }
}

#pragma warning restore S1133