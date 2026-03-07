using System;


namespace Spring.Client.Features.DualEntitySelection.Selectors;

/// <summary>
///     Selectors for deriving values from <see cref="DualEntitySelectionState" />.
/// </summary>
internal static class DualEntitySelectionSelectors
{
    /// <summary>
    ///     Selects the account A entity ID.
    /// </summary>
    /// <param name="state">The selection state.</param>
    /// <returns>The account A entity ID, or null if none.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="state" /> is null.
    /// </exception>
    public static string? GetAccountAId(
        DualEntitySelectionState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.AccountAId;
    }

    /// <summary>
    ///     Selects the account B entity ID.
    /// </summary>
    /// <param name="state">The selection state.</param>
    /// <returns>The account B entity ID, or null if none.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="state" /> is null.
    /// </exception>
    public static string? GetAccountBId(
        DualEntitySelectionState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.AccountBId;
    }

    /// <summary>
    ///     Selects whether both account IDs are present.
    /// </summary>
    /// <param name="state">The selection state.</param>
    /// <returns>True when both account IDs are set; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="state" /> is null.
    /// </exception>
    public static bool HasAccountPair(
        DualEntitySelectionState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return !string.IsNullOrWhiteSpace(state.AccountAId) && !string.IsNullOrWhiteSpace(state.AccountBId);
    }
}