using Microsoft.Extensions.DependencyInjection;

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
///             Dispatch(new AppInitAction(TimeProvider.System.GetUtcNow()));
///         }
///
///         protected override async Task OnInitializedAsync()
///         {
///             await LoadDataAsync();
///             Dispatch(new AppReadyAction(TimeProvider.System.GetUtcNow()));
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
    /// </remarks>
    public static IServiceCollection AddBuiltInLifecycle(
        this IServiceCollection services
    )
    {
        services.AddReducer<AppInitAction, LifecycleState>(LifecycleReducers.OnAppInit);
        services.AddReducer<AppReadyAction, LifecycleState>(LifecycleReducers.OnAppReady);
        return services;
    }
}