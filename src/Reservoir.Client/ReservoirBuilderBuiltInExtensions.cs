using System;

using Mississippi.Reservoir.Client.BuiltIn.Lifecycle.Actions;
using Mississippi.Reservoir.Client.BuiltIn.Lifecycle.Reducers;
using Mississippi.Reservoir.Client.BuiltIn.Lifecycle.State;
using Mississippi.Reservoir.Client.BuiltIn.Navigation.Actions;
using Mississippi.Reservoir.Client.BuiltIn.Navigation.Effects;
using Mississippi.Reservoir.Client.BuiltIn.Navigation.Reducers;
using Mississippi.Reservoir.Client.BuiltIn.Navigation.State;
using Mississippi.Reservoir.Core;


namespace Mississippi.Reservoir.Client;

/// <summary>
///     Extension methods for configuring built-in Blazor features on an <see cref="IReservoirBuilder" />.
/// </summary>
public static class ReservoirBuilderBuiltInExtensions
{
    /// <summary>
    ///     Adds all built-in Reservoir Blazor features (Navigation and Lifecycle).
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The same Reservoir builder for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="builder" /> is null.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         This registers:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <term>Navigation feature</term>
    ///             <description>
    ///                 <see cref="NavigationState" />, reducer for <see cref="LocationChangedAction" />,
    ///                 and <see cref="NavigationEffect" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>Lifecycle feature</term>
    ///             <description>
    ///                 <see cref="LifecycleState" />, reducers for <see cref="AppInitAction" />
    ///                 and <see cref="AppReadyAction" />.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         <strong>Important:</strong> You must also render the
    ///         <see cref="BuiltIn.Components.ReservoirNavigationProvider" /> component in your app
    ///         to receive location change notifications.
    ///     </para>
    /// </remarks>
    public static IReservoirBuilder AddBuiltIns(
        this IReservoirBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddNavigationFeature();
        builder.AddLifecycleFeature();
        return builder;
    }

    /// <summary>
    ///     Adds Reservoir DevTools integration to the Reservoir store.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <param name="configure">Optional configuration for DevTools options.</param>
    /// <returns>The same Reservoir builder for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="builder" /> is null.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         DevTools integration is opt-in and disabled by default.
    ///         To activate DevTools, add <c>&lt;ReservoirDevToolsInitializerComponent /&gt;</c>
    ///         to your root layout or <c>App.razor</c>.
    ///     </para>
    /// </remarks>
    public static IReservoirBuilder AddDevTools(
        this IReservoirBuilder builder,
        Action<ReservoirDevToolsOptions>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddReservoirDevTools(configure);
        return builder;
    }

    /// <summary>
    ///     Adds the built-in lifecycle feature to the Reservoir store.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The same Reservoir builder for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="builder" /> is null.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         Registers <see cref="LifecycleState" /> as a feature state with:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>Reducer for <see cref="AppInitAction" /></item>
    ///         <item>Reducer for <see cref="AppReadyAction" /></item>
    ///     </list>
    /// </remarks>
    public static IReservoirBuilder AddLifecycleFeature(
        this IReservoirBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddFeature<LifecycleState>(feature =>
        {
            feature.AddReducer<AppInitAction, LifecycleState>(LifecycleReducers.OnAppInit);
            feature.AddReducer<AppReadyAction, LifecycleState>(LifecycleReducers.OnAppReady);
        });
        return builder;
    }

    /// <summary>
    ///     Adds the built-in navigation feature to the Reservoir store.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The same Reservoir builder for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="builder" /> is null.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         Registers <see cref="NavigationState" /> as a feature state with:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>Reducer for <see cref="LocationChangedAction" /></item>
    ///         <item><see cref="NavigationEffect" /> for handling navigation actions</item>
    ///     </list>
    ///     <para>
    ///         <strong>Important:</strong> You must also render the
    ///         <see cref="BuiltIn.Components.ReservoirNavigationProvider" /> component in your app
    ///         to receive <see cref="LocationChangedAction" /> notifications.
    ///     </para>
    /// </remarks>
    public static IReservoirBuilder AddNavigationFeature(
        this IReservoirBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddFeature<NavigationState>(feature =>
        {
            feature.AddReducer<LocationChangedAction, NavigationState>(NavigationReducers.OnLocationChanged);
            feature.AddActionEffect<NavigationEffect, NavigationState>();
        });
        return builder;
    }
}