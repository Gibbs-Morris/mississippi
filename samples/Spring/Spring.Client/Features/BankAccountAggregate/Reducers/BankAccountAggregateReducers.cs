#if false
using Mississippi.Inlet.Blazor.WebAssembly.Abstractions;

using Spring.Client.Features.BankAccountAggregate.Actions;
using Spring.Client.Features.BankAccountAggregate.State;


namespace Spring.Client.Features.BankAccountAggregate.Reducers;

/// <summary>
///     Pure reducer functions for the BankAccountAggregate feature state.
/// </summary>
/// <remarks>
///     <para>
///         Each command has three lifecycle reducers (Executing, Failed, Succeeded) that delegate
///         to <see cref="AggregateCommandStateReducers" /> for the standard command tracking logic.
///     </para>
///     <para>
///         The <see cref="SetEntityId" /> reducer is application-specific for tracking
///         which entity is currently selected in the UI.
///     </para>
/// </remarks>
internal static class BankAccountAggregateReducers
{
    // DepositFunds reducers

    /// <summary>
    ///     Updates state when DepositFunds command starts executing.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action to reduce.</param>
    /// <returns>The new state with command tracked.</returns>
    public static BankAccountAggregateState DepositFundsExecuting(
        BankAccountAggregateState state,
        DepositFundsExecutingAction action
    ) =>
        AggregateCommandStateReducers.ReduceCommandExecuting(state, action);

    /// <summary>
    ///     Updates state when DepositFunds command fails.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action containing error details.</param>
    /// <returns>The new state with error populated.</returns>
    public static BankAccountAggregateState DepositFundsFailed(
        BankAccountAggregateState state,
        DepositFundsFailedAction action
    ) =>
        AggregateCommandStateReducers.ReduceCommandFailed(state, action);

    /// <summary>
    ///     Updates state when DepositFunds command succeeds.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action to reduce.</param>
    /// <returns>The new state with success set.</returns>
    public static BankAccountAggregateState DepositFundsSucceeded(
        BankAccountAggregateState state,
        DepositFundsSucceededAction action
    ) =>
        AggregateCommandStateReducers.ReduceCommandSucceeded(state, action);

    // OpenAccount reducers

    /// <summary>
    ///     Updates state when OpenAccount command starts executing.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action to reduce.</param>
    /// <returns>The new state with command tracked.</returns>
    public static BankAccountAggregateState OpenAccountExecuting(
        BankAccountAggregateState state,
        OpenAccountExecutingAction action
    ) =>
        AggregateCommandStateReducers.ReduceCommandExecuting(state, action);

    /// <summary>
    ///     Updates state when OpenAccount command fails.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action containing error details.</param>
    /// <returns>The new state with error populated.</returns>
    public static BankAccountAggregateState OpenAccountFailed(
        BankAccountAggregateState state,
        OpenAccountFailedAction action
    ) =>
        AggregateCommandStateReducers.ReduceCommandFailed(state, action);

    /// <summary>
    ///     Updates state when OpenAccount command succeeds.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action to reduce.</param>
    /// <returns>The new state with success set.</returns>
    public static BankAccountAggregateState OpenAccountSucceeded(
        BankAccountAggregateState state,
        OpenAccountSucceededAction action
    ) =>
        AggregateCommandStateReducers.ReduceCommandSucceeded(state, action);

    // Entity ID reducer

    /// <summary>
    ///     Sets the entity ID to target for commands.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action containing the entity ID.</param>
    /// <returns>The new state with the entity ID set.</returns>
    public static BankAccountAggregateState SetEntityId(
        BankAccountAggregateState state,
        SetEntityIdAction action
    ) =>
        state with
        {
            EntityId = action.EntityId,
        };

    // WithdrawFunds reducers

    /// <summary>
    ///     Updates state when WithdrawFunds command starts executing.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action to reduce.</param>
    /// <returns>The new state with command tracked.</returns>
    public static BankAccountAggregateState WithdrawFundsExecuting(
        BankAccountAggregateState state,
        WithdrawFundsExecutingAction action
    ) =>
        AggregateCommandStateReducers.ReduceCommandExecuting(state, action);

    /// <summary>
    ///     Updates state when WithdrawFunds command fails.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action containing error details.</param>
    /// <returns>The new state with error populated.</returns>
    public static BankAccountAggregateState WithdrawFundsFailed(
        BankAccountAggregateState state,
        WithdrawFundsFailedAction action
    ) =>
        AggregateCommandStateReducers.ReduceCommandFailed(state, action);

    /// <summary>
    ///     Updates state when WithdrawFunds command succeeds.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action to reduce.</param>
    /// <returns>The new state with success set.</returns>
    public static BankAccountAggregateState WithdrawFundsSucceeded(
        BankAccountAggregateState state,
        WithdrawFundsSucceededAction action
    ) =>
        AggregateCommandStateReducers.ReduceCommandSucceeded(state, action);
}
#endif