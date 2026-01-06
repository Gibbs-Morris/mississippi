using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Mississippi.Ripples.Abstractions;
using Mississippi.Ripples.Orleans.Grains;


namespace Mississippi.Ripples.Orleans;

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
///         and accessed by <see cref="RippleSubscriptionGrain" /> to resolve brook names
///         when clients subscribe to projections.
///     </para>
/// </remarks>
internal sealed class ProjectionBrookRegistry : IProjectionBrookRegistry
{
    private readonly ConcurrentDictionary<string, string> projectionToBrook = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public IEnumerable<string> GetAllProjectionTypes() => projectionToBrook.Keys;

    /// <inheritdoc />
    public string? GetBrookName(
        string projectionTypeName
    )
    {
        ArgumentNullException.ThrowIfNull(projectionTypeName);
        return projectionToBrook.TryGetValue(projectionTypeName, out string? brookName) ? brookName : null;
    }

    /// <inheritdoc />
    public void Register(
        string projectionTypeName,
        string brookName
    )
    {
        ArgumentNullException.ThrowIfNull(projectionTypeName);
        ArgumentNullException.ThrowIfNull(brookName);
        projectionToBrook[projectionTypeName] = brookName;
    }

    /// <inheritdoc />
    public bool TryGetBrookName(
        string projectionTypeName,
        out string? brookName
    )
    {
        ArgumentNullException.ThrowIfNull(projectionTypeName);
        return projectionToBrook.TryGetValue(projectionTypeName, out brookName);
    }
}