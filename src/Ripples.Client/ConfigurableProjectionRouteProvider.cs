namespace Mississippi.Ripples.Client;

using System;
using System.Collections.Generic;

using Mississippi.Ripples.Abstractions;

/// <summary>
/// A configurable implementation of <see cref="IProjectionRouteProvider"/> for manual route registration.
/// </summary>
/// <remarks>
/// Use this provider when source generators are not available or for testing.
/// In production with source generators, use the generated <c>ProjectionRouteRegistry</c> instead.
/// </remarks>
public sealed class ConfigurableProjectionRouteProvider : IProjectionRouteProvider
{
    private readonly Dictionary<Type, string> routes = new();

    /// <summary>
    /// Registers a route for a projection type.
    /// </summary>
    /// <typeparam name="TProjection">The type of projection.</typeparam>
    /// <param name="route">The API route path.</param>
    /// <returns>This instance for method chaining.</returns>
    public ConfigurableProjectionRouteProvider Register<TProjection>(string route)
    {
        ArgumentException.ThrowIfNullOrEmpty(route);
        routes[typeof(TProjection)] = route.TrimEnd('/');
        return this;
    }

    /// <summary>
    /// Registers a route for a projection type.
    /// </summary>
    /// <param name="projectionType">The type of projection.</param>
    /// <param name="route">The API route path.</param>
    /// <returns>This instance for method chaining.</returns>
    public ConfigurableProjectionRouteProvider Register(Type projectionType, string route)
    {
        ArgumentNullException.ThrowIfNull(projectionType);
        ArgumentException.ThrowIfNullOrEmpty(route);
        routes[projectionType] = route.TrimEnd('/');
        return this;
    }

    /// <inheritdoc/>
    public string GetRoute<TProjection>()
        => GetRoute(typeof(TProjection));

    /// <inheritdoc/>
    public string GetRoute(Type projectionType)
    {
        ArgumentNullException.ThrowIfNull(projectionType);

        if (routes.TryGetValue(projectionType, out string? route))
        {
            return route;
        }

        throw new InvalidOperationException(
            $"No route registered for projection type '{projectionType.Name}'. " +
            $"Register the route using {nameof(Register)} or use source generators.");
    }

    /// <inheritdoc/>
    public bool TryGetRoute<TProjection>(out string? route)
        => TryGetRoute(typeof(TProjection), out route);

    /// <inheritdoc/>
    public bool TryGetRoute(Type projectionType, out string? route)
    {
        ArgumentNullException.ThrowIfNull(projectionType);
        return routes.TryGetValue(projectionType, out route);
    }
}
