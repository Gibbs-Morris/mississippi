using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     Default implementation of <see cref="ISnapshotTypeRegistry" /> providing bidirectional
///     snapshot name ↔ CLR type resolution with O(1) lookup performance.
/// </summary>
/// <remarks>
///     <para>
///         This registry is populated at application startup through explicit registration
///         via <see cref="Register" /> or assembly scanning via <see cref="ScanAssembly" />.
///         Once populated, lookups are lock-free dictionary reads.
///     </para>
///     <para>
///         The registry maintains two parallel dictionaries for bidirectional lookup:
///         one mapping snapshot names to types, and another mapping types to snapshot names.
///     </para>
/// </remarks>
internal sealed class SnapshotTypeRegistry : ISnapshotTypeRegistry
{
    private readonly ConcurrentDictionary<string, Type> nameToType = new(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<Type, string> typeToName = new();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, Type> RegisteredTypes => nameToType;

    /// <inheritdoc />
    public void Register(
        string snapshotName,
        Type snapshotType
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotName);
        ArgumentNullException.ThrowIfNull(snapshotType);

        // Use TryAdd to avoid overwriting - first registration wins
        if (nameToType.TryAdd(snapshotName, snapshotType))
        {
            typeToName.TryAdd(snapshotType, snapshotName);
        }
    }

    /// <inheritdoc />
    public string? ResolveName(
        Type snapshotType
    )
    {
        ArgumentNullException.ThrowIfNull(snapshotType);
        return typeToName.TryGetValue(snapshotType, out string? name) ? name : null;
    }

    /// <inheritdoc />
    public Type? ResolveType(
        string snapshotTypeName
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotTypeName);
        return nameToType.TryGetValue(snapshotTypeName, out Type? snapshotType) ? snapshotType : null;
    }

    /// <inheritdoc />
    public int ScanAssembly(
        Assembly assembly
    )
    {
        ArgumentNullException.ThrowIfNull(assembly);
        int registeredCount = 0;
        foreach (Type type in assembly.GetTypes())
        {
            SnapshotStorageNameAttribute? attribute = type.GetCustomAttribute<SnapshotStorageNameAttribute>(false);
            if (attribute is null)
            {
                continue;
            }

            Register(attribute.StorageName, type);
            registeredCount++;
        }

        return registeredCount;
    }
}