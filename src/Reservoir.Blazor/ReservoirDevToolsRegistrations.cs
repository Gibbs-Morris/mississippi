using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Reservoir.Abstractions;


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
    ///     Adds Reservoir Redux DevTools integration to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for DevTools options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         Register after calling <c>AddReservoir</c>. DevTools integration is opt-in and disabled by default.
    ///     </para>
    ///     <para>
    ///         This registers a background service that subscribes to <c>IStore.StoreEvents</c> and
    ///         reports actions and state to the Redux DevTools browser extension. Time-travel commands
    ///         from DevTools are translated into system actions dispatched to the store.
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddReservoirDevTools(
        this IServiceCollection services,
        Action<ReservoirDevToolsOptions>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        if (configure is not null)
        {
            services.AddOptions<ReservoirDevToolsOptions>().Configure(configure);
        }
        else
        {
            services.AddOptions<ReservoirDevToolsOptions>();
        }

        // Singleton is safe because ReservoirDevToolsInterop only holds IJSRuntime which is
        // effectively singleton in Blazor WASM. Must be singleton to be injectable into
        // ReduxDevToolsService (hosted service = singleton).
        services.TryAddSingleton<ReservoirDevToolsInterop>();

        // Register Lazy<IStore> factory to allow deferred store resolution without injecting
        // IServiceProvider directly (avoids service locator anti-pattern per DI guidelines).
        services.TryAddSingleton<Lazy<IStore>>(sp => new(() => sp.GetRequiredService<IStore>()));
        services.AddHostedService<ReduxDevToolsService>();
        return services;
    }
}