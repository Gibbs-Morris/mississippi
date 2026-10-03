using System;


namespace Mississippi.Aqueduct.Gateway;

/// <summary>Protects the group namespace used for automatic user routing.</summary>
internal static class AqueductUserGroupNamespace
{
    /// <summary>The ordinal prefix reserved for user-routing groups.</summary>
    internal const string Prefix = "__aqueduct_user__";

    /// <summary>Rejects an ordinary group operation in the user-routing namespace.</summary>
    /// <param name="groupName">The ordinary group name.</param>
    /// <exception cref="ArgumentException">Thrown for a reserved user group name.</exception>
    internal static void ThrowIfReserved(
        string groupName
    )
    {
        if (groupName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Group names beginning with '{Prefix}' are reserved for user routing.",
                nameof(groupName));
        }
    }
}