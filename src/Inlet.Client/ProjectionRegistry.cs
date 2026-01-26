using System;
using System.Collections.Concurrent;

using Mississippi.Inlet.Abstractions;


namespace Mississippi.Inlet.Client;

/// <summary>
///     Default implementation of <see cref="Abstractions.IProjectionRegistry" />.
/// </summary>
public sealed class ProjectionRegistry : IProjectionRegistry
{
    private ConcurrentDictionary<Type, string> Paths { get; } = new();

    /// <inheritdoc />
    public string GetPath(
        Type projectionType
    )
    {
        ArgumentNullException.ThrowIfNull(projectionType);
        if (Paths.TryGetValue(projectionType, out string? path))
        {
            return path;
        }

        throw new InvalidOperationException($"No path registered for projection type {projectionType.Name}.");
    }

    /// <inheritdoc />
    public bool IsRegistered(
        Type projectionType
    )
    {
        ArgumentNullException.ThrowIfNull(projectionType);
        return Paths.ContainsKey(projectionType);
    }

    /// <inheritdoc />
    public void Register<T>(
        string path
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(path);
        Paths[typeof(T)] = path;
    }
}
