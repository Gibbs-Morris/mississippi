using System;


namespace Mississippi.Ripples.Abstractions;

/// <summary>
///     Provides routes for projection API endpoints.
/// </summary>
/// <remarks>
///     This interface allows the client to discover routes to projection endpoints.
///     In production, implementations are typically source-generated from grain attributes.
///     For testing or manual configuration, implement this interface directly.
/// </remarks>
public interface IProjectionRouteProvider
{
    /// <summary>
    ///     Gets the API route for the specified projection type.
    /// </summary>
    /// <typeparam name="TProjection">The type of projection.</typeparam>
    /// <returns>The route path without a trailing slash.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when no route is registered for the projection type.
    /// </exception>
    string GetRoute<TProjection>();

    /// <summary>
    ///     Gets the API route for the specified projection type.
    /// </summary>
    /// <param name="projectionType">The type of projection.</param>
    /// <returns>The route path without a trailing slash.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when no route is registered for the projection type.
    /// </exception>
    string GetRoute(
        Type projectionType
    );

    /// <summary>
    ///     Attempts to get the API route for the specified projection type.
    /// </summary>
    /// <typeparam name="TProjection">The type of projection.</typeparam>
    /// <param name="route">When this method returns, contains the route if found; otherwise, null.</param>
    /// <returns>True if the route was found; otherwise, false.</returns>
    bool TryGetRoute<TProjection>(
        out string? route
    );

    /// <summary>
    ///     Attempts to get the API route for the specified projection type.
    /// </summary>
    /// <param name="projectionType">The type of projection.</param>
    /// <param name="route">When this method returns, contains the route if found; otherwise, null.</param>
    /// <returns>True if the route was found; otherwise, false.</returns>
    bool TryGetRoute(
        Type projectionType,
        out string? route
    );
}