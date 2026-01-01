using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.EventSourcing.UxProjections.SignalR;

/// <summary>
///     Extension methods for registering UX projection SignalR services.
/// </summary>
public static class UxProjectionSignalRServiceCollectionExtensions
{
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