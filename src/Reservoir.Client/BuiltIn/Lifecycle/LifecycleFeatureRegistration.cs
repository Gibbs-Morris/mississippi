#pragma warning disable S1133 // Intentional staged deprecation pending issue #237.
using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Client.BuiltIn.Lifecycle.Actions;
using Mississippi.Reservoir.Client.BuiltIn.Lifecycle.Reducers;
using Mississippi.Reservoir.Client.BuiltIn.Lifecycle.State;
using Mississippi.Reservoir.Core;


namespace Mississippi.Reservoir.Client.BuiltIn.Lifecycle;

/// <summary>
///     Extension methods for registering the built-in lifecycle feature.
/// </summary>
/// <remarks>
///     <para>
///         The lifecycle feature provides Redux-style lifecycle state management for Blazor.
///         It tracks the current lifecycle phase (NotStarted, Initializing, Ready) in the store.
///     </para>
///     <para>
///         Register with <see cref="AddBuiltInLifecycle" /> after calling <c>AddReservoir</c>.
///     </para>
/// </remarks>
[Obsolete(
    "Legacy client composition entrypoint. Will be removed once GitHub issue #237 (Host/Sub-Builder Composition Model) is fully implemented. Migrate to ClientBuilder via UseMississippi() once available (see issue #237, in progress). See: https://github.com/Gibbs-Morris/mississippi/issues/237",
    false)]
public static class LifecycleFeatureRegistration
{
    /// <summary>
    ///     Adds the built-in lifecycle feature to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     This registers:
    ///     <list type="bullet">
    ///         <item><see cref="LifecycleState" /> as a feature state</item>
    ///         <item>Reducer for <see cref="AppInitAction" /></item>
    ///         <item>Reducer for <see cref="AppReadyAction" /></item>
    ///     </list>
    /// </remarks>
    [Obsolete(
        "Legacy client composition entrypoint. Will be removed once GitHub issue #237 (Host/Sub-Builder Composition Model) is fully implemented. Migrate to ClientBuilder via UseMississippi() once available (see issue #237, in progress). See: https://github.com/Gibbs-Morris/mississippi/issues/237",
        false)]
    public static IServiceCollection AddBuiltInLifecycle(
        this IServiceCollection services
    )
    {
        services.AddReducer<AppInitAction, LifecycleState>(LifecycleReducers.OnAppInit);
        services.AddReducer<AppReadyAction, LifecycleState>(LifecycleReducers.OnAppReady);
        return services;
    }
}

#pragma warning restore S1133