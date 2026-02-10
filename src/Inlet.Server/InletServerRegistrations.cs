using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Aqueduct;
using Mississippi.Aqueduct.Abstractions;
using Mississippi.Common.Abstractions.Builders;
using Mississippi.Inlet.Silo;


namespace Mississippi.Inlet.Server;

/// <summary>
///     Extension methods for registering Inlet Server services.
/// </summary>
/// <remarks>
///     <para>
///         Use these extensions on ASP.NET Core hosts that serve SignalR hubs.
///         For silo hosts, use <see cref="InletSiloRegistrations" />
///         from the <c>Inlet.Silo</c> package directly.
///     </para>
/// </remarks>
public static class InletServerRegistrations
{
    /// <summary>
    ///     Adds Inlet Orleans services with SignalR and the Orleans backplane.
    /// </summary>
    /// <param name="builder">The Mississippi server builder.</param>
    /// <param name="configureOptions">Optional action to configure Inlet Orleans options.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method registers SignalR with a custom Orleans-based backplane
    ///         that uses grains for connection tracking, group management, and
    ///         cross-server message routing.
    ///     </para>
    /// </remarks>
    public static IMississippiServerBuilder AddInletServer(
        this IMississippiServerBuilder builder,
        Action<InletServerOptions>? configureOptions = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services =>
        {
            services.AddSignalR();
            services.Configure(configureOptions ?? (_ => { }));
        });
        builder.AddInletSilo();
        builder.AddAqueduct().AddBackplane<InletHub>();
        return builder;
    }

    /// <summary>
    ///     Adds the <see cref="IAqueductNotifier" /> for ASP.NET-hosted services.
    /// </summary>
    /// <param name="builder">The Mississippi server builder.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         <strong>Important:</strong> This observer is for use on the ASP.NET pod only,
    ///         not for Orleans grains on silo pods. Grains cannot inject this service because
    ///         it requires <c>IHubContext</c> which only exists on ASP.NET pods.
    ///     </para>
    /// </remarks>
    public static IMississippiServerBuilder AddInletSignalRGrainObserver(
        this IMississippiServerBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddAqueduct().AddNotifier();
        return builder;
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