using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions.Attributes;


namespace Mississippi.EventSourcing.UxProjections.Api;

/// <summary>
///     Extension methods for mapping UX projection endpoints via minimal APIs.
/// </summary>
/// <remarks>
///     <para>
///         These methods enable automatic discovery and registration of UX projection
///         HTTP endpoints by scanning assemblies for types decorated with
///         <see cref="UxProjectionAttribute" />.
///     </para>
/// </remarks>
public static class UxProjectionEndpointExtensions
{
    private const string DefaultRoutePrefix = "api/projections";

    /// <summary>
    ///     Maps HTTP endpoints for all UX projections found in the specified assemblies.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="assemblies">
    ///     The assemblies to scan for projections. If empty, scans the entry assembly.
    /// </param>
    /// <returns>The endpoint route builder for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method scans the provided assemblies for types decorated with
    ///         <see cref="UxProjectionAttribute" /> and registers the following endpoints
    ///         for each projection:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <c>GET /api/projections/{route}/{entityId}</c> - Returns the latest projection state.
    ///         </item>
    ///         <item>
    ///             <c>GET /api/projections/{route}/{entityId}/version</c> - Returns the latest version.
    ///         </item>
    ///     </list>
    /// </remarks>
    public static IEndpointRouteBuilder MapUxProjections(
        this IEndpointRouteBuilder endpoints,
        params Assembly[] assemblies
    )
    {
        return endpoints.MapUxProjections(DefaultRoutePrefix, assemblies);
    }

    /// <summary>
    ///     Maps HTTP endpoints for all UX projections found in the specified assemblies
    ///     with a custom route prefix.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="routePrefix">The route prefix for all projection endpoints.</param>
    /// <param name="assemblies">
    ///     The assemblies to scan for projections. If empty, scans the entry assembly.
    /// </param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapUxProjections(
        this IEndpointRouteBuilder endpoints,
        string routePrefix,
        params Assembly[] assemblies
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(routePrefix);
        ArgumentNullException.ThrowIfNull(assemblies);

        IEnumerable<Assembly> assembliesToScan = assemblies.Length > 0
            ? assemblies
            : [Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()];

        List<(Type ProjectionType, UxProjectionAttribute Attribute)> projections = [];
        foreach (Assembly assembly in assembliesToScan)
        {
            foreach (Type type in assembly.GetTypes())
            {
                UxProjectionAttribute? attr = type.GetCustomAttribute<UxProjectionAttribute>();
                if (attr is not null)
                {
                    projections.Add((type, attr));
                }
            }
        }

        foreach ((Type projectionType, UxProjectionAttribute attr) in projections)
        {
            MapProjectionEndpoints(endpoints, routePrefix, projectionType, attr);
        }

        return endpoints;
    }

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
        where TProjection : class
    {
        return endpoints.MapUxProjection<TProjection>(DefaultRoutePrefix, route);
    }

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
            }).WithName($"Get{typeof(TProjection).Name}");

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
            }).WithName($"Get{typeof(TProjection).Name}Version");

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
            }).WithName($"Get{typeof(TProjection).Name}AtVersion");

        return group;
    }

    private static void MapProjectionEndpoints(
        IEndpointRouteBuilder endpoints,
        string routePrefix,
        Type projectionType,
        UxProjectionAttribute attribute
    )
    {
        string groupRoute = $"{routePrefix}/{attribute.Route}";
        RouteGroupBuilder group = endpoints.MapGroup(groupRoute);

        // We need to use reflection to call the generic grain factory method
        MethodInfo? getGrainMethod = typeof(IUxProjectionGrainFactory)
            .GetMethod(nameof(IUxProjectionGrainFactory.GetUxProjectionGrain))
            ?.MakeGenericMethod(projectionType);

        if (getGrainMethod is null)
        {
            return;
        }

        // GET {entityId} - Latest projection
        group.MapGet(
            "{entityId}",
            async (
                string entityId,
                IUxProjectionGrainFactory factory,
                HttpContext httpContext,
                CancellationToken cancellationToken
            ) =>
            {
                object? grainObj = getGrainMethod.Invoke(factory, [entityId]);
                if (grainObj is null)
                {
                    return Results.Problem("Failed to get grain");
                }

                // Get the grain interface methods via reflection
                Type grainType = grainObj.GetType();
                MethodInfo? getLatestVersionMethod = grainType.GetMethod("GetLatestVersionAsync");
                MethodInfo? getAsyncMethod = grainType.GetMethod("GetAsync");

                if (getLatestVersionMethod is null || getAsyncMethod is null)
                {
                    return Results.Problem("Failed to find grain methods");
                }

                // Get version for ETag
                Task<BrookPosition> versionTask =
                    (Task<BrookPosition>)getLatestVersionMethod.Invoke(grainObj, [cancellationToken])!;
                BrookPosition position = await versionTask;
                if (position.NotSet)
                {
                    return Results.NotFound();
                }

                string currentETag = $"\"{position.Value}\"";

                // Conditional GET
                string? ifNoneMatch = httpContext.Request.Headers.IfNoneMatch.ToString();
                if (!string.IsNullOrEmpty(ifNoneMatch) && (ifNoneMatch == currentETag))
                {
                    return Results.StatusCode(304);
                }

                // Get projection
                Task projectionTask = (Task)getAsyncMethod.Invoke(grainObj, [cancellationToken])!;
                await projectionTask;
                object? projection = projectionTask.GetType().GetProperty("Result")?.GetValue(projectionTask);
                if (projection is null)
                {
                    return Results.NotFound();
                }

                httpContext.Response.Headers.ETag = currentETag;
                httpContext.Response.Headers.CacheControl = "private, must-revalidate";
                return Results.Ok(projection);
            }).WithName($"Get{projectionType.Name}");

        // GET {entityId}/version
        group.MapGet(
            "{entityId}/version",
            async (
                string entityId,
                IUxProjectionGrainFactory factory,
                CancellationToken cancellationToken
            ) =>
            {
                object? grainObj = getGrainMethod.Invoke(factory, [entityId]);
                if (grainObj is null)
                {
                    return Results.Problem("Failed to get grain");
                }

                MethodInfo? getLatestVersionMethod = grainObj.GetType().GetMethod("GetLatestVersionAsync");
                if (getLatestVersionMethod is null)
                {
                    return Results.Problem("Failed to find GetLatestVersionAsync method");
                }

                Task<BrookPosition> versionTask =
                    (Task<BrookPosition>)getLatestVersionMethod.Invoke(grainObj, [cancellationToken])!;
                BrookPosition position = await versionTask;
                return position.NotSet ? Results.NotFound() : Results.Ok(position);
            }).WithName($"Get{projectionType.Name}Version");

        // GET {entityId}/at/{version}
        group.MapGet(
            "{entityId}/at/{version:long}",
            async (
                string entityId,
                long version,
                IUxProjectionGrainFactory factory,
                CancellationToken cancellationToken
            ) =>
            {
                object? grainObj = getGrainMethod.Invoke(factory, [entityId]);
                if (grainObj is null)
                {
                    return Results.Problem("Failed to get grain");
                }

                MethodInfo? getAtVersionMethod = grainObj.GetType().GetMethod("GetAtVersionAsync");
                if (getAtVersionMethod is null)
                {
                    return Results.Problem("Failed to find GetAtVersionAsync method");
                }

                BrookPosition brookPosition = new(version);
                Task projectionTask = (Task)getAtVersionMethod.Invoke(grainObj, [brookPosition, cancellationToken])!;
                await projectionTask;
                object? projection = projectionTask.GetType().GetProperty("Result")?.GetValue(projectionTask);
                return projection is null ? Results.NotFound() : Results.Ok(projection);
            }).WithName($"Get{projectionType.Name}AtVersion");
    }
}
