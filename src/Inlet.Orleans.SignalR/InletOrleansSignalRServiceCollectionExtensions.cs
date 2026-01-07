using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Aqueduct;
using Mississippi.Aqueduct.Abstractions;


namespace Mississippi.Inlet.Orleans.SignalR;

/// <summary>
///     Extension methods for registering Inlet Orleans SignalR services.
/// </summary>
/// <remarks>
///     <para>
///         Use these extensions on ASP.NET Core hosts that serve SignalR hubs.
///         For silo hosts, use <see cref="InletOrleansServiceCollectionExtensions" />
///         from the <c>Inlet.Orleans</c> package directly.
///     </para>
/// </remarks>
public static class InletOrleansSignalRServiceCollectionExtensions
{
    /// <summary>
    ///     Adds Inlet Orleans services with SignalR and the Orleans backplane.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">Optional action to configure Inlet Orleans options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method registers SignalR with a custom Orleans-based backplane
    ///         that uses grains for connection tracking, group management, and
    ///         cross-server message routing.
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddInletOrleansWithSignalR(
        this IServiceCollection services,
        Action<InletOrleansOptions>? configureOptions = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddInletOrleans();
        services.AddSignalR();
        services.Configure(configureOptions ?? (_ => { }));
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
    public static IServiceCollection AddInletSignalRGrainObserver(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddOrleansSignalRGrainObserver();
    }

    /// <summary>
    ///     Maps the Inlet hub to the specified path.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The URL pattern for the hub (default: "/hubs/inlet").</param>
    /// <returns>The hub endpoint convention builder for additional configuration.</returns>
    public static HubEndpointConventionBuilder MapInletHub(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/hubs/inlet"
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        return endpoints.MapHub<InletHub>(pattern);
    }
}