using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Reservoir.Client;

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
    public static IReservoirBuilder AddReservoirDevTools(
        this IReservoirBuilder builder,
        Action<ReservoirDevToolsOptions>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        IServiceCollection services = builder.Services;
        if (configure is not null)
        {
            services.AddOptions<ReservoirDevToolsOptions>().Configure(configure);
        }
        else
        {
            services.AddOptions<ReservoirDevToolsOptions>();
        }

        services.TryAddSingleton<DevToolsInitializationTracker>();
        services.AddHostedService<DevToolsInitializationCheckerService>();
        services.TryAddScoped<ReservoirDevToolsInterop>();
        services.TryAddScoped<ReduxDevToolsService>();
        return builder;
    }
}