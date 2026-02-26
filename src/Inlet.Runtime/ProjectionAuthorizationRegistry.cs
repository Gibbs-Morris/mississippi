using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Mississippi.Inlet.Runtime.Abstractions;


namespace Mississippi.Inlet.Runtime;

/// <summary>
///     Thread-safe implementation of <see cref="IProjectionAuthorizationRegistry" />.
/// </summary>
internal sealed class ProjectionAuthorizationRegistry : IProjectionAuthorizationRegistry
{
    private ConcurrentDictionary<string, ProjectionAuthorizationMetadata> PathToAuthorizationMetadata { get; } =
        new(StringComparer.Ordinal);

    /// <inheritdoc />
    public IEnumerable<string> GetAllPaths() => PathToAuthorizationMetadata.Keys;

    /// <inheritdoc />
    public ProjectionAuthorizationMetadata? GetAuthorizationMetadata(
        string path
    )
    {
        ArgumentNullException.ThrowIfNull(path);
        return PathToAuthorizationMetadata.TryGetValue(path, out ProjectionAuthorizationMetadata? metadata) ? metadata : null;
    }

    /// <inheritdoc />
    public void Register(
        string path,
        ProjectionAuthorizationMetadata metadata
    )
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(metadata);
        PathToAuthorizationMetadata[path] = metadata;
    }
}