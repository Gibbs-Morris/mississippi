using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Mississippi.Inlet.Abstractions;


namespace Mississippi.Inlet.Orleans;

/// <summary>
///     Thread-safe implementation of <see cref="IProjectionBrookRegistry" />.
/// </summary>
/// <remarks>
///     <para>
///         This registry maintains a mapping from projection type names to their
///         associated brook names. It is populated at application startup by scanning
///         assemblies for types decorated with <c>[UxProjection]</c> attributes.
///     </para>
///     <para>
///         The registry is designed to be registered as a singleton in the DI container
///         and accessed by <see cref="Grains.InletSubscriptionGrain" /> to resolve brook names
///         when clients subscribe to projections.
///     </para>
/// </remarks>
internal sealed class ProjectionBrookRegistry : IProjectionBrookRegistry
{
    private ConcurrentDictionary<string, string> ProjectionToBrook { get; } = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public IEnumerable<string> GetAllProjectionTypes() => ProjectionToBrook.Keys;

    /// <inheritdoc />
    public string? GetBrookName(
        string projectionTypeName
    )
    {
        ArgumentNullException.ThrowIfNull(projectionTypeName);
        return ProjectionToBrook.TryGetValue(projectionTypeName, out string? brookName) ? brookName : null;
    }

    /// <inheritdoc />
    public void Register(
        string projectionTypeName,
        string brookName
    )
    {
        ArgumentNullException.ThrowIfNull(projectionTypeName);
        ArgumentNullException.ThrowIfNull(brookName);
        ProjectionToBrook[projectionTypeName] = brookName;
    }

    /// <inheritdoc />
    public bool TryGetBrookName(
        string projectionTypeName,
        out string? brookName
    )
    {
        ArgumentNullException.ThrowIfNull(projectionTypeName);
        return ProjectionToBrook.TryGetValue(projectionTypeName, out brookName);
    }
}