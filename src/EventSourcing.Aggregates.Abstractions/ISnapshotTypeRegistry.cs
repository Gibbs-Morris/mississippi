using System;
using System.Collections.Generic;
using System.Reflection;

using Mississippi.EventSourcing.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Provides bidirectional resolution between snapshot/state types and their string names.
/// </summary>
/// <remarks>
///     <para>
///         The snapshot type registry is built at application startup and provides O(1) lookups
///         for both name-to-type and type-to-name resolution. This enables efficient
///         deserialization during snapshot loading and serialization when persisting snapshots.
///     </para>
///     <para>
///         Snapshot types are identified by their <see cref="SnapshotNameAttribute" />, which provides
///         a stable string identity that survives type renames and namespace changes.
///     </para>
/// </remarks>
public interface ISnapshotTypeRegistry
{
    /// <summary>
    ///     Gets all registered snapshot types.
    /// </summary>
    IReadOnlyDictionary<string, Type> RegisteredTypes { get; }

    /// <summary>
    ///     Registers a snapshot type with its corresponding snapshot name.
    /// </summary>
    /// <param name="snapshotName">The snapshot name as defined by <see cref="SnapshotNameAttribute" />.</param>
    /// <param name="snapshotType">The CLR type of the snapshot.</param>
    void Register(
        string snapshotName,
        Type snapshotType
    );

    /// <summary>
    ///     Resolves the snapshot name for a CLR type.
    /// </summary>
    /// <param name="snapshotType">The CLR type of the snapshot.</param>
    /// <returns>The snapshot name, or null if the type is not registered.</returns>
    string? ResolveName(
        Type snapshotType
    );

    /// <summary>
    ///     Resolves a snapshot type from its string name.
    /// </summary>
    /// <param name="snapshotTypeName">The snapshot type name to resolve.</param>
    /// <returns>The CLR type of the snapshot, or null if not found.</returns>
    Type? ResolveType(
        string snapshotTypeName
    );

    /// <summary>
    ///     Scans an assembly for types decorated with <see cref="SnapshotNameAttribute" /> and registers them.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The number of snapshot types registered from the assembly.</returns>
    int ScanAssembly(
        Assembly assembly
    );
}