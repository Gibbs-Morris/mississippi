using System;


namespace Mississippi.Aqueduct.Runtime;

/// <summary>Removes reserved user-routing names from Aqueduct log values.</summary>
internal static class AqueductGroupLogValue
{
    private const string UserGroupMarker = "[user-group]";

    private const string UserGroupPrefix = "__aqueduct_user__";

    /// <summary>Masks the reserved group component of a composite grain key for logging.</summary>
    /// <param name="groupKey">The actual composite key used by the group grain.</param>
    /// <returns>The hub name and a fixed marker, or the unchanged ordinary group key.</returns>
    internal static string ForKey(
        string groupKey
    )
    {
        ReadOnlySpan<char> key = groupKey.AsSpan();
        int separatorIndex = key.IndexOf(':');
        if ((separatorIndex < 0) || !key[(separatorIndex + 1)..].StartsWith(UserGroupPrefix, StringComparison.Ordinal))
        {
            return groupKey;
        }

        return string.Concat(key[..(separatorIndex + 1)], UserGroupMarker);
    }

    /// <summary>Masks a reserved user-routing group name for logging.</summary>
    /// <param name="groupName">The actual group name used for routing.</param>
    /// <returns>A fixed marker for a reserved user group, or the unchanged ordinary group name.</returns>
    internal static string ForName(
        string groupName
    ) =>
        groupName.AsSpan().StartsWith(UserGroupPrefix, StringComparison.Ordinal) ? UserGroupMarker : groupName;
}