using System;


namespace Spring.Client.Features.DemoAccounts;

/// <summary>
///     Selectors for deriving values from <see cref="DemoAccountsState" />.
/// </summary>
internal static class DemoAccountsSelectors
{
    /// <summary>
    ///     Gets the demo account A identifier.
    /// </summary>
    /// <param name="state">The demo accounts state.</param>
    /// <returns>The demo account A identifier.</returns>
    public static string? GetAccountAId(
        DemoAccountsState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.AccountAId;
    }

    /// <summary>
    ///     Gets the demo account A display name.
    /// </summary>
    /// <param name="state">The demo accounts state.</param>
    /// <returns>The demo account A display name.</returns>
    public static string? GetAccountAName(
        DemoAccountsState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.AccountAName;
    }

    /// <summary>
    ///     Gets the demo account B identifier.
    /// </summary>
    /// <param name="state">The demo accounts state.</param>
    /// <returns>The demo account B identifier.</returns>
    public static string? GetAccountBId(
        DemoAccountsState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.AccountBId;
    }

    /// <summary>
    ///     Gets the demo account B display name.
    /// </summary>
    /// <param name="state">The demo accounts state.</param>
    /// <returns>The demo account B display name.</returns>
    public static string? GetAccountBName(
        DemoAccountsState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.AccountBName;
    }

    /// <summary>
    ///     Creates a selector that returns the other demo account identifier.
    /// </summary>
    /// <param name="selectedEntityId">The currently selected entity ID.</param>
    /// <returns>The other demo account ID, or null if not applicable.</returns>
    public static Func<DemoAccountsState, string?> GetOtherAccountId(
        string? selectedEntityId
    ) =>
        state =>
        {
            ArgumentNullException.ThrowIfNull(state);
            if (string.IsNullOrWhiteSpace(selectedEntityId))
            {
                return null;
            }

            if (string.Equals(selectedEntityId, state.AccountAId, StringComparison.Ordinal))
            {
                return state.AccountBId;
            }

            if (string.Equals(selectedEntityId, state.AccountBId, StringComparison.Ordinal))
            {
                return state.AccountAId;
            }

            return null;
        };

    /// <summary>
    ///     Gets a value indicating whether both demo accounts are initialized.
    /// </summary>
    /// <param name="state">The demo accounts state.</param>
    /// <returns><c>true</c> when both demo accounts are initialized; otherwise <c>false</c>.</returns>
    public static bool IsInitialized(
        DemoAccountsState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return !string.IsNullOrWhiteSpace(state.AccountAId) && !string.IsNullOrWhiteSpace(state.AccountBId);
    }
}