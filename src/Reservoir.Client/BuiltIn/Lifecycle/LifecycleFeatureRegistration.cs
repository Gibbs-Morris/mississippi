using System;

using Mississippi.Reservoir.Abstractions;
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
/// </remarks>
public static class LifecycleFeatureRegistration
{
    /// <summary>
    ///     Adds the built-in lifecycle feature to the service collection.
    /// </summary>
    /// <param name="reservoir">The Reservoir builder.</param>
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
        this IReservoirBuilder reservoir
    )
    {
        ArgumentNullException.ThrowIfNull(reservoir);
        reservoir.AddFeature<LifecycleState>(feature => feature
            .AddReducer<LifecycleState, AppInitAction>(LifecycleReducers.OnAppInit)
            .AddReducer<LifecycleState, AppReadyAction>(LifecycleReducers.OnAppReady));
        return reservoir;
    }
}