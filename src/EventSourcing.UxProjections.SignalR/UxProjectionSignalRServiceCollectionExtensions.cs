using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Viaduct;


namespace Mississippi.EventSourcing.UxProjections.SignalR;

/// <summary>
///     Extension methods for registering UX projection SignalR services.
/// </summary>
public static class UxProjectionSignalRServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the Orleans backplane to an existing SignalR configuration.
    /// </summary>
    /// <param name="builder">The SignalR server builder.</param>
    /// <param name="configureOptions">Optional action to configure backplane options.</param>
    /// <returns>The SignalR server builder for chaining.</returns>
    public static ISignalRServerBuilder AddOrleansBackplane(
        this ISignalRServerBuilder builder,
        Action<OrleansBackplaneOptions>? configureOptions = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.Configure(configureOptions ?? (_ => { }));
        builder.Services.TryAddSingleton(typeof(HubLifetimeManager<>), typeof(OrleansHubLifetimeManager<>));
        return builder;
    }

    /// <summary>
    ///     Adds the <see cref="ISignalRGrainObserver" /> for ASP.NET-hosted services to send SignalR messages.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         <strong>Important:</strong> This observer is for use on the ASP.NET pod only,
    ///         not for Orleans grains on silo pods. Grains cannot inject this service because
    ///         it requires <c>IHubContext</c> which only exists on ASP.NET pods.
    ///     </para>
    ///     <para>
    ///         Grains that need to send SignalR messages should call
    ///         <c>ISignalRGroupGrain.SendMessageAsync</c> directly instead. The group grain
    ///         publishes to an Orleans stream that ASP.NET pods subscribe to, bridging the
    ///         silo-to-web boundary.
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddSignalRGrainObserver(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddOrleansSignalRGrainObserver();
    }

    /// <summary>
    ///     Adds UX projection SignalR services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method registers the SignalR hub and related services for
    ///         real-time projection notifications. The SignalR infrastructure
    ///         works with the custom Orleans backplane for distributed routing.
    ///     </para>
    ///     <para>
    ///         Call <see cref="MapUxProjectionHub" /> on the endpoint builder
    ///         to map the hub endpoint.
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddUxProjectionSignalR(
        this IServiceCollection services
    )
    {
        services.AddSignalR();
        return services;
    }

    /// <summary>
    ///     Adds UX projection SignalR services with the Orleans backplane.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">Optional action to configure backplane options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method registers SignalR with a custom Orleans-based backplane
    ///         that uses grains for connection tracking, group management, and
    ///         cross-server message routing.
    ///     </para>
    ///     <para>
    ///         The Orleans backplane eliminates the need for Redis or Azure SignalR
    ///         by using the Orleans cluster as the backplane infrastructure.
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddUxProjectionSignalRWithOrleansBackplane(
        this IServiceCollection services,
        Action<OrleansBackplaneOptions>? configureOptions = null
    )
    {
        services.AddSignalR();
        services.Configure(configureOptions ?? (_ => { }));

        // Replace the default HubLifetimeManager with Orleans-based implementation
        services.TryAddSingleton(typeof(HubLifetimeManager<>), typeof(OrleansHubLifetimeManager<>));
        return services;
    }

    /// <summary>
    ///     Maps the UX projection hub to the specified path.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The URL pattern for the hub (default: "/hubs/projections").</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapUxProjectionHub(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/hubs/projections"
    )
    {
        endpoints.MapHub<UxProjectionHub>(pattern);
        return endpoints;
    }
}