using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Viaduct;


namespace Mississippi.Ripples.Orleans.SignalR;

/// <summary>
///     Extension methods for registering Ripples Orleans SignalR services.
/// </summary>
/// <remarks>
///     <para>
///         Use these extensions on ASP.NET Core hosts that serve SignalR hubs.
///         For silo hosts, use <see cref="RipplesOrleansServiceCollectionExtensions" />
///         from the <c>Ripples.Orleans</c> package directly.
///     </para>
/// </remarks>
public static class RipplesOrleansSignalRServiceCollectionExtensions
{
    /// <summary>
    ///     Adds Ripples Orleans services with SignalR and the Orleans backplane.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">Optional action to configure Ripple Orleans options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method registers SignalR with a custom Orleans-based backplane
    ///         that uses grains for connection tracking, group management, and
    ///         cross-server message routing.
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddRipplesOrleansWithSignalR(
        this IServiceCollection services,
        Action<RippleOrleansOptions>? configureOptions = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddRipplesOrleans();
        services.AddSignalR();
        services.Configure(configureOptions ?? (_ => { }));

        // Replace the default HubLifetimeManager with Orleans-based implementation
        services.TryAddSingleton(typeof(HubLifetimeManager<>), typeof(OrleansHubLifetimeManager<>));
        return services;
    }

    /// <summary>
    ///     Adds the <see cref="ISignalRGrainObserver" /> for ASP.NET-hosted services.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         <strong>Important:</strong> This observer is for use on the ASP.NET pod only,
    ///         not for Orleans grains on silo pods. Grains cannot inject this service because
    ///         it requires <c>IHubContext</c> which only exists on ASP.NET pods.
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddRipplesSignalRGrainObserver(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddOrleansSignalRGrainObserver();
    }

    /// <summary>
    ///     Maps the Ripple hub to the specified path.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The URL pattern for the hub (default: "/hubs/ripples").</param>
    /// <returns>The hub endpoint convention builder for additional configuration.</returns>
    public static HubEndpointConventionBuilder MapRippleHub(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/hubs/ripples"
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        return endpoints.MapHub<RippleHub>(pattern);
    }
}