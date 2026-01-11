using System;


namespace Mississippi.Inlet.Abstractions.Configuration;

/// <summary>
///     Attribute to specify the projection route for a projection type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ProjectionRouteAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionRouteAttribute" /> class.
    /// </summary>
    /// <param name="route">The route path for this projection.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="route" /> is null or empty.</exception>
    public ProjectionRouteAttribute(
        string route
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(route);
        Route = route;
    }

    /// <summary>
    ///     Gets the route path for this projection.
    /// </summary>
    public string Route { get; }
}