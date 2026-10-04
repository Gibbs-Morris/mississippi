using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.Tributary.Abstractions.Attributes;


namespace Mississippi.Tributary.Abstractions;

/// <summary>
///     Configuration options for snapshot retention and replay strategies.
/// </summary>
/// <remarks>
///     <para>
///         Snapshots are retained at intervals defined by <see cref="DefaultRetainModulus" />.
///         When building state for a given version, the system finds the nearest retained snapshot
///         (floor division of version by modulus) and replays only the delta events.
///     </para>
///     <para>
///         For example, with a modulus of 50:
///         <list type="bullet">
///             <item>Version 364 → base snapshot at 350, replay 14 events</item>
///             <item>Version 199 → base snapshot at 150, replay 49 events</item>
///             <item>Version 50 → base snapshot at 0, replay events after position 0</item>
///         </list>
///     </para>
///     <para>
///         Use <see cref="StateTypeOverrides" /> to configure different retention intervals
///         for specific state types that may need more or fewer snapshots based on their
///         complexity or access patterns.
///     </para>
/// </remarks>
public sealed class SnapshotRetentionOptions
{
    private static readonly ConcurrentDictionary<Type, SnapshotTypeMetadata> SnapshotTypeMetadataCache = new();

    /// <summary>
    ///     Gets or sets the default snapshot retention modulus.
    ///     Snapshots are retained at positions divisible by this value.
    /// </summary>
    /// <value>The default modulus for snapshot retention. Defaults to 50.</value>
    /// <remarks>
    ///     A modulus of 50 means snapshots are retained at positions 0, 50, 100, 150, etc.
    ///     This limits event replay to at most <c>modulus - 1</c> events when building state.
    /// </remarks>
    public int DefaultRetainModulus { get; set; } = 50;

    /// <summary>
    ///     Gets or sets a value indicating whether every reconstructed snapshot is persisted.
    /// </summary>
    /// <value><c>true</c> to persist every reconstructed snapshot; otherwise, <c>false</c>. Defaults to <c>false</c>.</value>
    /// <remarks>
    ///     This setting changes persistence eligibility only. It does not change the replay interval.
    /// </remarks>
    public bool ShouldPersistAllSnapshots { get; set; }

    /// <summary>
    ///     Gets the collection of per-state-type retention modulus overrides.
    /// </summary>
    /// <value>A dictionary mapping state type snapshot names to their retention modulus values.</value>
    /// <remarks>
    ///     Use this to configure specific state types that need different snapshot intervals.
    ///     For example, complex aggregates with expensive state computation might use a smaller
    ///     modulus (e.g., 50) to reduce replay cost, while simple aggregates might use a larger
    ///     modulus (e.g., 200) to reduce storage overhead.
    ///     Keys should be the <see cref="SnapshotStorageNameAttribute.StorageName" /> value
    ///     (e.g., "MYAPP.DOMAIN.COUNTERSTATE.V1") for refactoring safety.
    /// </remarks>
    public Dictionary<string, int> StateTypeOverrides { get; } = new(StringComparer.Ordinal);

    private static int EnsurePositiveModulus(
        int modulus,
        string source
    )
    {
        if (modulus <= 0)
        {
            throw new InvalidOperationException(
                $"Snapshot retention modulus from '{source}' must be greater than zero, but was {modulus}.");
        }

        return modulus;
    }

    private static SnapshotTypeMetadata GetSnapshotTypeMetadata(
        Type stateType
    ) =>
        SnapshotTypeMetadataCache.GetOrAdd(
            stateType,
            static type => new(
                type.GetCustomAttribute<SnapshotStorageNameAttribute>(false)?.StorageName,
                type.GetCustomAttribute<SnapshotRetentionAttribute>(false)?.Modulus));

    /// <summary>
    ///     Calculates the base snapshot version for a given target version.
    /// </summary>
    /// <typeparam name="TSnapshot">The state type to calculate the base version for.</typeparam>
    /// <param name="targetVersion">The target version to find the base snapshot for.</param>
    /// <returns>
    ///     The nearest retained snapshot version that is strictly less than the target version.
    ///     Returns 0 if the target version is less than or equal to the modulus.
    /// </returns>
    /// <remarks>
    ///     For example, with a modulus of 50:
    ///     <list type="bullet">
    ///         <item>Target 364 → base 350</item>
    ///         <item>Target 199 → base 150</item>
    ///         <item>Target 50 → base 0 (not 50, to prevent self-reference)</item>
    ///         <item>Target 49 → base 0</item>
    ///     </list>
    /// </remarks>
    public long GetBaseSnapshotVersion<TSnapshot>(
        long targetVersion
    ) =>
        GetBaseSnapshotVersion(typeof(TSnapshot), targetVersion);

    /// <summary>
    ///     Calculates the base snapshot version for a given target version.
    /// </summary>
    /// <param name="stateType">The state type to calculate the base version for.</param>
    /// <param name="targetVersion">The target version to find the base snapshot for.</param>
    /// <returns>
    ///     The nearest retained snapshot version that is strictly less than the target version.
    ///     Returns 0 if the target version is less than or equal to the modulus.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stateType" /> is null.</exception>
    /// <remarks>
    ///     The base version must be strictly less than the target to prevent a grain from
    ///     calling itself when the target version equals a retention boundary.
    ///     For example, with modulus 5: target 5 → base 0, target 10 → base 5, target 7 → base 5.
    /// </remarks>
    public long GetBaseSnapshotVersion(
        Type stateType,
        long targetVersion
    )
    {
        ArgumentNullException.ThrowIfNull(stateType);
        if (targetVersion <= 0)
        {
            return 0;
        }

        int modulus = GetRetainModulus(stateType);

        // Use (targetVersion - 1) to ensure the base is strictly less than the target.
        // This prevents self-referential grain calls when targetVersion % modulus == 0.
        // Examples with modulus 5:
        //   target 5: (5-1)/5*5 = 0  (not 5, which would cause self-call)
        //   target 6: (6-1)/5*5 = 5
        //   target 10: (10-1)/5*5 = 5  (not 10)
        //   target 11: (11-1)/5*5 = 10
        return ((targetVersion - 1) / modulus) * modulus;
    }

    /// <summary>
    ///     Gets the retention modulus for a specific state type.
    /// </summary>
    /// <typeparam name="TSnapshot">The state type to get the modulus for.</typeparam>
    /// <returns>
    ///     The configured modulus for the state's stable snapshot storage name or CLR type name if an override
    ///     exists; otherwise, the state's <see cref="SnapshotRetentionAttribute.Modulus" /> or
    ///     <see cref="DefaultRetainModulus" />.
    /// </returns>
    /// <remarks>
    ///     Resolves overrides in this order: stable snapshot storage name, CLR full name, the
    ///     <see cref="SnapshotRetentionAttribute.Modulus" /> attribute, then <see cref="DefaultRetainModulus" />.
    /// </remarks>
    public int GetRetainModulus<TSnapshot>() => GetRetainModulus(typeof(TSnapshot));

    /// <summary>
    ///     Gets the retention modulus for a specific state type.
    /// </summary>
    /// <param name="stateType">The state type to get the modulus for.</param>
    /// <returns>
    ///     The configured modulus for the state type if an override exists;
    ///     otherwise, the state type's <see cref="SnapshotRetentionAttribute.Modulus" /> or
    ///     <see cref="DefaultRetainModulus" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stateType" /> is null.</exception>
    /// <remarks>
    ///     Looks up overrides in this order: stable <see cref="SnapshotStorageNameAttribute.StorageName" />,
    ///     <see cref="Type.FullName" />, <see cref="SnapshotRetentionAttribute.Modulus" />, then
    ///     <see cref="DefaultRetainModulus" />. Attribute metadata is discovered once per type and cached.
    /// </remarks>
    public int GetRetainModulus(
        Type stateType
    )
    {
        ArgumentNullException.ThrowIfNull(stateType);
        SnapshotTypeMetadata metadata = GetSnapshotTypeMetadata(stateType);

        // Prefer the stable snapshot name when available.
        if (metadata.SnapshotStorageName is not null &&
            StateTypeOverrides.TryGetValue(metadata.SnapshotStorageName, out int modulusBySnapshotName))
        {
            return EnsurePositiveModulus(modulusBySnapshotName, metadata.SnapshotStorageName);
        }

        // Fall back to CLR type name for backward compatibility.
        string typeName = stateType.FullName ?? stateType.Name;
        if (StateTypeOverrides.TryGetValue(typeName, out int modulusByTypeName))
        {
            return EnsurePositiveModulus(modulusByTypeName, typeName);
        }

        return EnsurePositiveModulus(metadata.AttributeModulus ?? DefaultRetainModulus, typeName);
    }

    /// <summary>
    ///     Determines whether a snapshot version is eligible for persistence.
    /// </summary>
    /// <typeparam name="TSnapshot">The state type being persisted.</typeparam>
    /// <param name="version">The zero-based brook position of the reconstructed state.</param>
    /// <returns><c>true</c> when the version is selected by the policy; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="version" /> is negative.</exception>
    public bool ShouldPersistSnapshot<TSnapshot>(
        long version
    ) =>
        ShouldPersistSnapshot(typeof(TSnapshot), version);

    /// <summary>
    ///     Determines whether a snapshot version is eligible for persistence.
    /// </summary>
    /// <param name="stateType">The state type being persisted.</param>
    /// <param name="version">The zero-based brook position of the reconstructed state.</param>
    /// <returns><c>true</c> when the version is selected by the policy; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stateType" /> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="version" /> is negative.</exception>
    public bool ShouldPersistSnapshot(
        Type stateType,
        long version
    )
    {
        ArgumentNullException.ThrowIfNull(stateType);
        ArgumentOutOfRangeException.ThrowIfNegative(version);
        int modulus = GetRetainModulus(stateType);
        return ShouldPersistAllSnapshots || ((version % modulus) == 0);
    }

    private sealed record SnapshotTypeMetadata(string? SnapshotStorageName, int? AttributeModulus);
}