using System;
using System.Collections.Concurrent;

using Mississippi.Inlet.Abstractions;


namespace Mississippi.Inlet;

/// <summary>
///     Default implementation of <see cref="Abstractions.IProjectionRegistry" />.
/// </summary>
public sealed class ProjectionRegistry : IProjectionRegistry
{
    private ConcurrentDictionary<Type, string> Routes { get; } = new();

    /// <inheritdoc />
    public string GetRoute(
        Type projectionType
    )
    {
        ArgumentNullException.ThrowIfNull(projectionType);
        if (Routes.TryGetValue(projectionType, out string? route))
        {
            return route;
        }

        throw new InvalidOperationException($"No route registered for projection type {projectionType.Name}.");
    }

    /// <inheritdoc />
    public bool IsRegistered(
        Type projectionType
    )
    {
        ArgumentNullException.ThrowIfNull(projectionType);
        return Routes.ContainsKey(projectionType);
    }

    /// <inheritdoc />
    public void Register<T>(
        string route
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(route);
        Routes[typeof(T)] = route;
    }
}