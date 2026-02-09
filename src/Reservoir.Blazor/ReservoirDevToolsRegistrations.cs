using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Reservoir.Abstractions.Builders;

namespace Mississippi.Reservoir.Blazor;

/// <summary>
///     Extension methods for registering Reservoir DevTools integration.
/// </summary>
/// <remarks>
///     Justification: public to provide opt-in registration from application startup.
/// </remarks>
public static class ReservoirDevToolsRegistrations
{
    /// <summary>
    ///     Adds Reservoir Redux DevTools integration to the Reservoir builder.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <param name="configure">Optional configuration for DevTools options.</param>
    /// <returns>The Reservoir builder for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         Register after calling <c>AddReservoir</c>. DevTools integration is opt-in and disabled by default.
    ///     </para>
    ///     <para>
    ///         This registers a scoped <see cref="ReduxDevToolsService" /> that subscribes to
    ///         <c>IStore.StoreEvents</c> and reports actions and state to the Redux DevTools browser
    ///         extension. Time-travel commands from DevTools are translated into system actions
    ///         dispatched to the store.
    ///     </para>
    ///     <para>
    ///         To activate DevTools, add <c>&lt;ReservoirDevToolsInitializerComponent /&gt;</c> to your root
    ///         layout or <c>App.razor</c>. This component initializes DevTools after the Blazor
    ///         rendering context is available.
    ///     </para>
    ///     <para>
    ///         When DevTools is enabled (not <see cref="ReservoirDevToolsEnablement.Off" />), a background
    ///         check runs after startup. If <see cref="ReservoirDevToolsInitializerComponent" /> has not
    ///         initialized DevTools, a warning is logged to help diagnose missing component configuration.
    ///     </para>
    /// </remarks>
    public static IReservoirBuilder AddReservoirDevTools(
        this IReservoirBuilder builder,
        Action<ReservoirDevToolsOptions>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services =>
        {
            if (configure is not null)
            {
                services.AddOptions<ReservoirDevToolsOptions>().Configure(configure);
            }
            else
            {
                services.AddOptions<ReservoirDevToolsOptions>();
            }

            // Singleton tracker for cross-scope communication between scoped service and hosted service
            services.TryAddSingleton<DevToolsInitializationTracker>();

            // Hosted service to check if initialization was called
            services.AddHostedService<DevToolsInitializationCheckerService>();

            // Scoped to match IStore lifetime. In Blazor Server, each circuit gets its own scope.
            // In Blazor WASM, the single scope acts as a pseudo-singleton.
            services.TryAddScoped<ReservoirDevToolsInterop>();
            services.TryAddScoped<ReduxDevToolsService>();
        });
        return builder;
    }
}