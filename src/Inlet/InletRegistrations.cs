using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Abstractions.Configuration;
using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Inlet;

/// <summary>
///     Extension methods for registering Inlet services.
/// </summary>
public static class InletRegistrations
{
    /// <summary>
    ///     Adds Inlet services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services" /> is null.</exception>
    public static IServiceCollection AddInlet(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddInlet(_ => { });
    }

    /// <summary>
    ///     Adds Inlet services to the service collection with configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for Inlet options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="services" /> or <paramref name="configure" /> is null.
    /// </exception>
    public static IServiceCollection AddInlet(
        this IServiceCollection services,
        Action<InletOptions> configure
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        services.Configure(configure);
        services.TryAddSingleton<IProjectionRegistry, ProjectionRegistry>();
        services.TryAddSingleton<InletStore>();
        services.TryAddSingleton<IStore>(sp => sp.GetRequiredService<InletStore>());
        services.TryAddSingleton<IInletStore>(sp => sp.GetRequiredService<InletStore>());
        services.TryAddSingleton<IProjectionUpdateNotifier>(sp => sp.GetRequiredService<InletStore>());
        return services;
    }

    /// <summary>
    ///     Registers a projection route.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="route">The route path.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="services" /> or <paramref name="route" /> is null.
    /// </exception>
    public static IServiceCollection AddProjectionRoute<T>(
        this IServiceCollection services,
        string route
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(route);
        services.AddSingleton<IConfigureProjectionRegistry>(new ProjectionRouteRegistration<T>(route));
        return services;
    }
}