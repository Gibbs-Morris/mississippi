using System;
using System.Collections.Generic;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Snapshots.Abstractions;

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
///         For example, with a modulus of 100:
///         <list type="bullet">
///             <item>Version 364 → base snapshot at 300, replay 64 events</item>
///             <item>Version 199 → base snapshot at 100, replay 99 events</item>
///             <item>Version 50 → no base snapshot, replay all 50 events from start</item>
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
    /// <summary>
    ///     Gets or sets the default snapshot retention modulus.
    ///     Snapshots are retained at positions divisible by this value.
    /// </summary>
    /// <value>The default modulus for snapshot retention. Defaults to 100.</value>
    /// <remarks>
    ///     A modulus of 100 means snapshots are retained at positions 0, 100, 200, 300, etc.
    ///     This limits event replay to at most <c>modulus - 1</c> events when building state.
    /// </remarks>
    public int DefaultRetainModulus { get; set; } = 100;

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
    /// <example>
    ///     <code>
    /// options.StateTypeOverrides["MYAPP.DOMAIN.COMPLEXSTATE.V1"] = 50;
    /// options.StateTypeOverrides["MYAPP.DOMAIN.SIMPLESTATE.V1"] = 200;
    /// </code>
    /// </example>
    public Dictionary<string, int> StateTypeOverrides { get; } = new(StringComparer.Ordinal);

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
    ///     For example, with a modulus of 100:
    ///     <list type="bullet">
    ///         <item>Target 364 → base 300</item>
    ///         <item>Target 199 → base 100</item>
    ///         <item>Target 100 → base 0 (not 100, to prevent self-reference)</item>
    ///         <item>Target 99 → base 0</item>
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
    ///     The configured modulus for the state type if an override exists;
    ///     otherwise, <see cref="DefaultRetainModulus" />.
    /// </returns>
    public int GetRetainModulus<TSnapshot>() => GetRetainModulus(typeof(TSnapshot));

    /// <summary>
    ///     Gets the retention modulus for a specific state type.
    /// </summary>
    /// <param name="stateType">The state type to get the modulus for.</param>
    /// <returns>
    ///     The configured modulus for the state type if an override exists;
    ///     otherwise, <see cref="DefaultRetainModulus" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stateType" /> is null.</exception>
    /// <remarks>
    ///     Looks up by <see cref="SnapshotStorageNameAttribute.StorageName" /> first if the type
    ///     is decorated with the attribute; falls back to <see cref="Type.FullName" /> for
    ///     backward compatibility.
    /// </remarks>
    public int GetRetainModulus(
        Type stateType
    )
    {
        ArgumentNullException.ThrowIfNull(stateType);

        // Prefer the stable snapshot name when available
        if (SnapshotStorageNameHelper.TryGetStorageName(stateType, out string? snapshotName) &&
            StateTypeOverrides.TryGetValue(snapshotName!, out int modulusBySnapshotName))
        {
            return modulusBySnapshotName;
        }

        // Fall back to CLR type name for backward compatibility
        string typeName = stateType.FullName ?? stateType.Name;
        return StateTypeOverrides.TryGetValue(typeName, out int modulus) ? modulus : DefaultRetainModulus;
    }
}