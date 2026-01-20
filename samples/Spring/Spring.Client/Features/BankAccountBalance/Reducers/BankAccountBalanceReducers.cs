using Mississippi.Common.Abstractions.Attributes;

using Spring.Client.Features.BankAccountBalance.Actions;
using Spring.Client.Features.BankAccountBalance.State;


namespace Spring.Client.Features.BankAccountBalance.Reducers;

/// <summary>
///     Pure reducer functions for the BankAccountBalance projection feature state.
/// </summary>
[PendingSourceGenerator]
internal static class BankAccountBalanceReducers
{
    /// <summary>
    ///     Sets error state when the fetch fails.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action containing the error message.</param>
    /// <returns>The new state with the error populated.</returns>
    public static BankAccountBalanceState FetchFailed(
        BankAccountBalanceState state,
        BankAccountBalanceFetchFailedAction action
    ) =>
        state with
        {
            IsLoading = false,
            ErrorMessage = action.ErrorMessage,
        };

    /// <summary>
    ///     Updates state with loaded projection data.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action containing projection data.</param>
    /// <returns>The new state with projection data populated.</returns>
    public static BankAccountBalanceState Loaded(
        BankAccountBalanceState state,
        BankAccountBalanceLoadedAction action
    ) =>
        state with
        {
            IsLoading = false,
            AccountId = action.AccountId,
            Balance = action.Balance,
            HolderName = action.HolderName,
            IsOpen = action.IsOpen,
            ErrorMessage = null,
        };

    /// <summary>
    ///     Sets loading state when fetch starts.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action to reduce.</param>
    /// <returns>The new state with loading set to true.</returns>
    public static BankAccountBalanceState Loading(
        BankAccountBalanceState state,
        BankAccountBalanceLoadingAction action
    ) =>
        state with
        {
            IsLoading = true,
            ErrorMessage = null,
        };
}
