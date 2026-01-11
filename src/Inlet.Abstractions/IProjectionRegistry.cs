using System;


namespace Mississippi.Inlet.Abstractions;

/// <summary>
///     Registry for projection types and their associated routes.
/// </summary>
public interface IProjectionRegistry
{
    /// <summary>
    ///     Gets the route for a projection type.
    /// </summary>
    /// <param name="projectionType">The projection type.</param>
    /// <returns>The route path for the projection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="projectionType" /> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no route is registered for the type.</exception>
    string GetRoute(
        Type projectionType
    );

    /// <summary>
    ///     Gets whether a projection type is registered.
    /// </summary>
    /// <param name="projectionType">The projection type.</param>
    /// <returns>True if the projection type has a registered route.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="projectionType" /> is null.</exception>
    bool IsRegistered(
        Type projectionType
    );

    /// <summary>
    ///     Registers a route for a projection type.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="route">The route path.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="route" /> is null.</exception>
    void Register<T>(
        string route
    )
        where T : class;
}