using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;


namespace Mississippi.EventSourcing.UxProjections.Api;

/// <summary>
///     Extension methods for mapping UX projection endpoints via minimal APIs.
/// </summary>
/// <remarks>
///     <para>
///         These methods enable explicit registration of individual UX projection HTTP endpoints.
///         For automatic endpoint generation, use the <c>[GenerateProjectionEndpoints]</c> attribute
///         which generates controllers at compile time via source generators.
///     </para>
/// </remarks>
public static class UxProjectionEndpointExtensions
{
    private const string DefaultRoutePrefix = "api/projections";

    /// <summary>
    ///     Maps HTTP endpoints for a single UX projection type.
    /// </summary>
    /// <typeparam name="TProjection">The projection type.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="route">The route segment for this projection.</param>
    /// <returns>A route group builder for further customization.</returns>
    public static RouteGroupBuilder MapUxProjection<TProjection>(
        this IEndpointRouteBuilder endpoints,
        string route
    )
        where TProjection : class =>
        endpoints.MapUxProjection<TProjection>(DefaultRoutePrefix, route);

    /// <summary>
    ///     Maps HTTP endpoints for a single UX projection type with a custom route prefix.
    /// </summary>
    /// <typeparam name="TProjection">The projection type.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="routePrefix">The route prefix for the projection endpoints.</param>
    /// <param name="route">The route segment for this projection.</param>
    /// <returns>A route group builder for further customization.</returns>
    public static RouteGroupBuilder MapUxProjection<TProjection>(
        this IEndpointRouteBuilder endpoints,
        string routePrefix,
        string route
    )
        where TProjection : class
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(routePrefix);
        ArgumentNullException.ThrowIfNull(route);
        string groupRoute = $"{routePrefix}/{route}";
        RouteGroupBuilder group = endpoints.MapGroup(groupRoute);
        group.MapGet(
                "{entityId}",
                async (
                    string entityId,
                    IUxProjectionGrainFactory factory,
                    HttpContext httpContext,
                    ILogger<TProjection> logger,
                    CancellationToken cancellationToken
                ) =>
                {
                    IUxProjectionGrain<TProjection> grain = factory.GetUxProjectionGrain<TProjection>(entityId);

                    // Get latest version for ETag
                    BrookPosition position = await grain.GetLatestVersionAsync(cancellationToken);
                    if (position.NotSet)
                    {
                        return Results.NotFound();
                    }

                    string currentETag = $"\"{position.Value}\"";

                    // Conditional GET support
                    string? ifNoneMatch = httpContext.Request.Headers.IfNoneMatch.ToString();
                    if (!string.IsNullOrEmpty(ifNoneMatch) && (ifNoneMatch == currentETag))
                    {
                        return Results.StatusCode(304);
                    }

                    TProjection? projection = await grain.GetAsync(cancellationToken);
                    if (projection is null)
                    {
                        return Results.NotFound();
                    }

                    httpContext.Response.Headers.ETag = currentETag;
                    httpContext.Response.Headers.CacheControl = "private, must-revalidate";
                    return Results.Ok(projection);
                })
            .WithName($"Get{typeof(TProjection).Name}");
        group.MapGet(
                "{entityId}/version",
                async (
                    string entityId,
                    IUxProjectionGrainFactory factory,
                    CancellationToken cancellationToken
                ) =>
                {
                    IUxProjectionGrain<TProjection> grain = factory.GetUxProjectionGrain<TProjection>(entityId);
                    BrookPosition position = await grain.GetLatestVersionAsync(cancellationToken);
                    return position.NotSet ? Results.NotFound() : Results.Ok(position);
                })
            .WithName($"Get{typeof(TProjection).Name}Version");
        group.MapGet(
                "{entityId}/at/{version:long}",
                async (
                    string entityId,
                    long version,
                    IUxProjectionGrainFactory factory,
                    CancellationToken cancellationToken
                ) =>
                {
                    IUxProjectionGrain<TProjection> grain = factory.GetUxProjectionGrain<TProjection>(entityId);
                    BrookPosition brookPosition = new(version);
                    TProjection? projection = await grain.GetAtVersionAsync(brookPosition, cancellationToken);
                    return projection is null ? Results.NotFound() : Results.Ok(projection);
                })
            .WithName($"Get{typeof(TProjection).Name}AtVersion");
        return group;
    }
}