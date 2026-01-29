using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
///         <strong>Usage:</strong>
///     </para>
///     <code>
///         services.AddReservoir();
///         services.AddBuiltInLifecycle();
///
///         // In your root component:
///         protected override void OnInitialized()
///         {
///             Dispatch(new AppInitAction());
///         }
///
///         protected override async Task OnInitializedAsync()
///         {
///             await LoadDataAsync();
///             Dispatch(new AppReadyAction());
///         }
///     </code>
/// </remarks>
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
    ///     <para>
    ///         If <see cref="TimeProvider" /> is not already registered, this method
    ///         registers <see cref="TimeProvider.System" /> as the default.
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddBuiltInLifecycle(
        this IServiceCollection services
    )
    {
        // Ensure TimeProvider is registered for deterministic timestamps
        services.TryAddSingleton(TimeProvider.System);

        // Register reducers that resolve TimeProvider via DI
        services.AddReducer<AppInitAction, LifecycleState, AppInitReducer>();
        services.AddReducer<AppReadyAction, LifecycleState, AppReadyReducer>();
        return services;
    }

    /// <summary>
    ///     Adds the built-in lifecycle feature with a custom time provider.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="timeProvider">The time provider to use for timestamps.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBuiltInLifecycle(
        this IServiceCollection services,
        TimeProvider timeProvider
    )
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        services.AddSingleton(timeProvider);
        services.AddReducer<AppInitAction, LifecycleState, AppInitReducer>();
        services.AddReducer<AppReadyAction, LifecycleState, AppReadyReducer>();
        return services;
    }
}