using System;
using System.Collections.Generic;


namespace Mississippi.Inlet.Abstractions;

/// <summary>
///     Registry that maps projection paths to their brook names.
/// </summary>
/// <remarks>
///     <para>
///         This registry provides a runtime lookup for path-to-brook mappings,
///         allowing the subscription system to determine which brook stream to subscribe
///         to for a given projection path. The registry is populated at startup by
///         scanning assemblies for types decorated with <c>[ProjectionPath]</c> attributes.
///     </para>
///     <para>
///         This abstraction allows clients to subscribe to projections by path
///         without needing to know the underlying brook structure, maintaining a clean
///         separation between the UX projection layer and the event sourcing infrastructure.
///     </para>
/// </remarks>
public interface IProjectionBrookRegistry
{
    /// <summary>
    ///     Gets all registered projection paths.
    /// </summary>
    /// <returns>An enumerable of all registered projection paths.</returns>
    IEnumerable<string> GetAllPaths();

    /// <summary>
    ///     Gets the brook name for a projection path.
    /// </summary>
    /// <param name="path">The projection path (e.g., "cascade/channels").</param>
    /// <returns>The brook name for the projection, or null if not registered.</returns>
    string? GetBrookName(
        string path
    );

    /// <summary>
    ///     Registers a projection path with its brook name.
    /// </summary>
    /// <param name="path">The projection path.</param>
    /// <param name="brookName">The brook name to associate with the projection.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="path" /> or <paramref name="brookName" /> is null.
    /// </exception>
    void Register(
        string path,
        string brookName
    );

    /// <summary>
    ///     Tries to get the brook name for a projection path.
    /// </summary>
    /// <param name="path">The projection path.</param>
    /// <param name="brookName">The brook name if found.</param>
    /// <returns>True if the projection path is registered; otherwise, false.</returns>
    bool TryGetBrookName(
        string path,
        out string? brookName
    );
}