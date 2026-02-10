using System;

using Mississippi.Reservoir.Abstractions.Builders;
using Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.Actions;
using Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.Reducers;
using Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.State;


namespace Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle;

/// <summary>
///     Extension methods for registering the built-in lifecycle feature.
/// </summary>
/// <remarks>
///     <para>
///         The lifecycle feature provides Redux-style lifecycle state management for Blazor.
///         It tracks the current lifecycle phase (NotStarted, Initializing, Ready) in the store.
///     </para>
///     <para>
///         Register with <see cref="AddBuiltInLifecycle" /> after calling <c>AddReservoir</c> on the builder.
///     </para>
/// </remarks>
public static class LifecycleFeatureRegistration
{
    /// <summary>
    ///     Adds the built-in lifecycle feature to the Reservoir builder.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The Reservoir builder for chaining.</returns>
    /// <remarks>
    ///     This registers:
    ///     <list type="bullet">
    ///         <item><see cref="LifecycleState" /> as a feature state</item>
    ///         <item>Reducer for <see cref="AppInitAction" /></item>
    ///         <item>Reducer for <see cref="AppReadyAction" /></item>
    ///     </list>
    /// </remarks>
    public static IReservoirBuilder AddBuiltInLifecycle(
        this IReservoirBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddFeature<LifecycleState>(featureBuilder =>
        {
            featureBuilder.AddReducer<AppInitAction>(LifecycleReducers.OnAppInit);
            featureBuilder.AddReducer<AppReadyAction>(LifecycleReducers.OnAppReady);
        });
        return builder;
    }
}