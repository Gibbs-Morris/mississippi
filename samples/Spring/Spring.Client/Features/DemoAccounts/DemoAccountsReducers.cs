namespace Spring.Client.Features.DemoAccounts;

/// <summary>
///     Reducers for the demo accounts feature state.
/// </summary>
internal static class DemoAccountsReducers
{
    /// <summary>
    ///     Sets the demo account identifiers and names.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action containing the demo account data.</param>
    /// <returns>The updated state.</returns>
    public static DemoAccountsState SetDemoAccounts(
        DemoAccountsState state,
        SetDemoAccountsAction action
    ) =>
        state with
        {
            AccountAId = action.AccountAId,
            AccountAName = action.AccountAName,
            AccountBId = action.AccountBId,
            AccountBName = action.AccountBName,
        };
}