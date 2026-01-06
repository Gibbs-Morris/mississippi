using System;
using System.Collections.Generic;


namespace Mississippi.Ripples.Abstractions;

/// <summary>
///     Registry that maps projection types to their brook names.
/// </summary>
/// <remarks>
///     <para>
///         This registry provides a runtime lookup for projection-to-brook mappings,
///         allowing the subscription system to determine which brook stream to subscribe
///         to for a given projection type. The registry is populated at startup by
///         scanning assemblies for types decorated with <c>[UxProjection]</c> attributes.
///     </para>
///     <para>
///         This abstraction allows clients to subscribe to projections by type name
///         without needing to know the underlying brook structure, maintaining a clean
///         separation between the UX projection layer and the event sourcing infrastructure.
///     </para>
/// </remarks>
public interface IProjectionBrookRegistry
{
    /// <summary>
    ///     Gets all registered projection type names.
    /// </summary>
    /// <returns>An enumerable of all registered projection type names.</returns>
    IEnumerable<string> GetAllProjectionTypes();

    /// <summary>
    ///     Gets the brook name for a projection type.
    /// </summary>
    /// <param name="projectionTypeName">The projection type name (typically the class name).</param>
    /// <returns>The brook name for the projection, or null if not registered.</returns>
    string? GetBrookName(
        string projectionTypeName
    );

    /// <summary>
    ///     Registers a projection type with its brook name.
    /// </summary>
    /// <param name="projectionTypeName">The projection type name.</param>
    /// <param name="brookName">The brook name to associate with the projection.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="projectionTypeName" /> or <paramref name="brookName" /> is null.
    /// </exception>
    void Register(
        string projectionTypeName,
        string brookName
    );

    /// <summary>
    ///     Attempts to get the brook name for a projection type.
    /// </summary>
    /// <param name="projectionTypeName">The projection type name.</param>
    /// <param name="brookName">
    ///     When this method returns, contains the brook name if found; otherwise, null.
    /// </param>
    /// <returns>True if the projection type was found; otherwise, false.</returns>
    bool TryGetBrookName(
        string projectionTypeName,
        out string? brookName
    );
}