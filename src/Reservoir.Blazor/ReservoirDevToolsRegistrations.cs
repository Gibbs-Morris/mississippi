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
    ///     Register after calling <c>AddReservoir</c>. DevTools integration is opt-in and disabled by default.
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

        services.TryAddScoped<ReservoirDevToolsInterop>();
        services.Replace(ServiceDescriptor.Scoped<IStore, ReservoirDevToolsStore>());
        return services;
    }
}
