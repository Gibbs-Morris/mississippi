using System;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Client.BuiltIn.Lifecycle.Actions;
using Mississippi.Reservoir.Client.BuiltIn.Lifecycle.Reducers;
using Mississippi.Reservoir.Client.BuiltIn.Lifecycle.State;


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
///         Register with <see cref="AddBuiltInLifecycle(IReservoirBuilder)" /> after calling
///         <c>AddReservoir</c>.
///     </para>
/// </remarks>
public static class LifecycleFeatureRegistration
{
    /// <summary>
    ///     Adds the built-in lifecycle feature to the Reservoir builder.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The Reservoir builder for chaining.</returns>
    public static IReservoirBuilder AddBuiltInLifecycle(
        this IReservoirBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddFeatureState<LifecycleState>(feature => feature
            .AddReducer<AppInitAction>(LifecycleReducers.OnAppInit)
            .AddReducer<AppReadyAction>(LifecycleReducers.OnAppReady));
        return builder;
    }
}