using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions.Attributes;
using Mississippi.Inlet.Projection.Abstractions;


namespace Mississippi.EventSourcing.UxProjections.Api;

/// <summary>
///     Extension methods for mapping UX projection endpoints via minimal APIs.
/// </summary>
/// <remarks>
///     <para>
///         These methods enable automatic discovery and registration of UX projection
///         HTTP endpoints by scanning assemblies for types decorated with
///         <see cref="ProjectionPathAttribute" /> and optionally <see cref="UxProjectionAttribute" />
///         for HTTP-specific configuration.
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
    ///         <see cref="ProjectionPathAttribute" /> and registers the following endpoints
    ///         for each projection:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <c>GET /api/projections/{path}/{entityId}</c> - Returns the latest projection state.
    ///         </item>
    ///         <item>
    ///             <c>GET /api/projections/{path}/{entityId}/version</c> - Returns the latest version.
    ///         </item>
    ///     </list>
    ///     <para>
    ///         If the type also has <see cref="UxProjectionAttribute" />, authorization and tags
    ///         from that attribute are applied to the endpoints.
    ///     </para>
    /// </remarks>
    public static IEndpointRouteBuilder MapUxProjections(
        this IEndpointRouteBuilder endpoints,
        params Assembly[] assemblies
    ) =>
        endpoints.MapUxProjections(DefaultRoutePrefix, assemblies);

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
        List<(Type ProjectionType, string Path, UxProjectionAttribute HttpConfig)> projections = [];
        foreach (Assembly assembly in assembliesToScan)
        {
            foreach (Type type in assembly.GetTypes())
            {
                // Both attributes required: ProjectionPath for the route, UxProjection to expose via HTTP
                ProjectionPathAttribute? pathAttr = type.GetCustomAttribute<ProjectionPathAttribute>();
                UxProjectionAttribute? httpConfig = type.GetCustomAttribute<UxProjectionAttribute>();
                if (pathAttr is null || httpConfig is null)
                {
                    continue;
                }

                projections.Add((type, pathAttr.Path, httpConfig));
            }
        }

        foreach ((Type projectionType, string path, UxProjectionAttribute _) in projections)
        {
            MapProjectionEndpoints(endpoints, routePrefix, projectionType, path);
        }

        return endpoints;
    }

    private static async Task<object?> ExtractResultFromValueTaskAsync(
        object valueTaskResult
    )
    {
        // ValueTask doesn't have Result property directly accessible, convert to Task first
        Task projectionTask = (Task)valueTaskResult.GetType().GetMethod("AsTask")!.Invoke(valueTaskResult, null)!;
        await projectionTask;
        return projectionTask.GetType().GetProperty("Result")?.GetValue(projectionTask);
    }

    private static async Task<object?> InvokeProjectionAtVersionAsync(
        MethodInfo method,
        object grainObj,
        BrookPosition position,
        CancellationToken cancellationToken
    )
    {
        object projectionResult = method.Invoke(grainObj, [position, cancellationToken])!;
        return await ExtractResultFromValueTaskAsync(projectionResult);
    }

    private static async Task<object?> InvokeProjectionMethodAsync(
        MethodInfo method,
        object grainObj,
        CancellationToken cancellationToken
    )
    {
        object projectionResult = method.Invoke(grainObj, [cancellationToken])!;
        return await ExtractResultFromValueTaskAsync(projectionResult);
    }

    private static async Task<BrookPosition> InvokeVersionMethodAsync(
        MethodInfo method,
        object grainObj,
        CancellationToken cancellationToken
    )
    {
        object versionResult = method.Invoke(grainObj, [cancellationToken])!;
        return await (ValueTask<BrookPosition>)versionResult;
    }

    private static bool IsNotModified(
        HttpContext httpContext,
        string currentETag
    )
    {
        string? ifNoneMatch = httpContext.Request.Headers.IfNoneMatch.ToString();
        return !string.IsNullOrEmpty(ifNoneMatch) && (ifNoneMatch == currentETag);
    }

    private static void MapAtVersionEndpoint(
        RouteGroupBuilder group,
        Type projectionType,
        MethodInfo getGrainMethod,
        MethodInfo getAtVersionMethod
    )
    {
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

                    BrookPosition brookPosition = new(version);
                    object? projection = await InvokeProjectionAtVersionAsync(
                        getAtVersionMethod,
                        grainObj,
                        brookPosition,
                        cancellationToken);
                    return projection is null ? Results.NotFound() : Results.Ok(projection);
                })
            .WithName($"Get{projectionType.Name}AtVersion");
    }

    private static void MapLatestProjectionEndpoint(
        RouteGroupBuilder group,
        Type projectionType,
        MethodInfo getGrainMethod,
        MethodInfo getLatestVersionMethod,
        MethodInfo getAsyncMethod
    )
    {
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

                    BrookPosition position = await InvokeVersionMethodAsync(
                        getLatestVersionMethod,
                        grainObj,
                        cancellationToken);
                    if (position.NotSet)
                    {
                        return Results.NotFound();
                    }

                    string currentETag = $"\"{position.Value}\"";
                    if (IsNotModified(httpContext, currentETag))
                    {
                        return Results.StatusCode(304);
                    }

                    object? projection = await InvokeProjectionMethodAsync(getAsyncMethod, grainObj, cancellationToken);
                    if (projection is null)
                    {
                        return Results.NotFound();
                    }

                    httpContext.Response.Headers.ETag = currentETag;
                    httpContext.Response.Headers.CacheControl = "private, must-revalidate";
                    return Results.Ok(projection);
                })
            .WithName($"Get{projectionType.Name}");
    }

    private static void MapProjectionEndpoints(
        IEndpointRouteBuilder endpoints,
        string routePrefix,
        Type projectionType,
        string path
    )
    {
        string groupRoute = $"{routePrefix}/{path}";
        RouteGroupBuilder group = endpoints.MapGroup(groupRoute);

        // We need to use reflection to call the generic grain factory method
        MethodInfo? getGrainMethod = typeof(IUxProjectionGrainFactory)
            .GetMethod(nameof(IUxProjectionGrainFactory.GetUxProjectionGrain))
            ?.MakeGenericMethod(projectionType);
        if (getGrainMethod is null)
        {
            return;
        }

        // Get grain interface methods once at setup time (Orleans proxies don't expose methods directly)
        Type grainInterfaceType = typeof(IUxProjectionGrain<>).MakeGenericType(projectionType);
        MethodInfo getLatestVersionMethod = grainInterfaceType.GetMethod("GetLatestVersionAsync")!;
        MethodInfo getAsyncMethod = grainInterfaceType.GetMethod("GetAsync")!;
        MethodInfo getAtVersionMethod = grainInterfaceType.GetMethod("GetAtVersionAsync")!;
        MapLatestProjectionEndpoint(group, projectionType, getGrainMethod, getLatestVersionMethod, getAsyncMethod);
        MapVersionEndpoint(group, projectionType, getGrainMethod, getLatestVersionMethod);
        MapAtVersionEndpoint(group, projectionType, getGrainMethod, getAtVersionMethod);
    }

    private static void MapVersionEndpoint(
        RouteGroupBuilder group,
        Type projectionType,
        MethodInfo getGrainMethod,
        MethodInfo getLatestVersionMethod
    )
    {
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

                    BrookPosition position = await InvokeVersionMethodAsync(
                        getLatestVersionMethod,
                        grainObj,
                        cancellationToken);
                    return position.NotSet ? Results.NotFound() : Results.Ok(position.Value);
                })
            .WithName($"Get{projectionType.Name}Version");
    }
}