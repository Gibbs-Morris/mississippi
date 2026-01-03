namespace Mississippi.Ripples.Client;

using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Ripples.Abstractions;

/// <summary>
/// Extension methods for registering Ripples client services.
/// </summary>
public static class RipplesClientServiceCollectionExtensions
{
    /// <summary>
    /// Adds Ripples client services for Blazor WebAssembly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure client options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddRipplesClient(
        this IServiceCollection services,
        Action<RipplesClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);

        // Register SignalR connection as singleton for shared connection
        services.TryAddSingleton<ISignalRRippleConnection, SignalRRippleConnection>();

        // Register configurable route provider if not already registered
        services.TryAddSingleton<IProjectionRouteProvider>(
            new ConfigurableProjectionRouteProvider());

        return services;
    }

    /// <summary>
    /// Adds Ripples client services with a custom route provider.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure client options.</param>
    /// <param name="routeProvider">The route provider instance.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddRipplesClient(
        this IServiceCollection services,
        Action<RipplesClientOptions> configure,
        IProjectionRouteProvider routeProvider)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(routeProvider);

        services.Configure(configure);
        services.TryAddSingleton<ISignalRRippleConnection, SignalRRippleConnection>();
        services.AddSingleton(routeProvider);

        return services;
    }

    /// <summary>
    /// Registers a client ripple factory for the specified projection type.
    /// </summary>
    /// <typeparam name="TProjection">The type of projection.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddClientRipple<TProjection>(this IServiceCollection services)
        where TProjection : class
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IRippleFactory<TProjection>, ClientRippleFactory<TProjection>>();

        return services;
    }

    /// <summary>
    /// Registers a client ripple pool for the specified projection type.
    /// </summary>
    /// <typeparam name="TProjection">The type of projection.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddClientRipplePool<TProjection>(this IServiceCollection services)
        where TProjection : class
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IRipplePool<TProjection>, ClientRipplePool<TProjection>>();

        return services;
    }

    /// <summary>
    /// Configures the route provider with projection routes.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure routes.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection ConfigureProjectionRoutes(
        this IServiceCollection services,
        Action<ConfigurableProjectionRouteProvider> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        ConfigurableProjectionRouteProvider provider = new();
        configure(provider);
        services.AddSingleton<IProjectionRouteProvider>(provider);

        return services;
    }
}
