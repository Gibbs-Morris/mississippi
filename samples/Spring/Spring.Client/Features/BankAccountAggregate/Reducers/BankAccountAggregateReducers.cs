using System.Collections.Immutable;

using Mississippi.Inlet.Blazor.WebAssembly.Abstractions;
using Mississippi.Inlet.Blazor.WebAssembly.Abstractions.Commands;
using Mississippi.Sdk.Generators.Abstractions;

using Spring.Client.Features.BankAccountAggregate.Actions;
using Spring.Client.Features.BankAccountAggregate.State;


namespace Spring.Client.Features.BankAccountAggregate.Reducers;

/// <summary>
///     Pure reducer functions for the BankAccountAggregate feature state.
/// </summary>
[PendingSourceGenerator]
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
    )
    {
        (ImmutableHashSet<string> inFlight, ImmutableList<CommandHistoryEntry> history) =
            AggregateCommandStateReducers.ComputeCommandExecuting(state, action);
        return state with
        {
            InFlightCommands = inFlight,
            CommandHistory = history,
            ErrorCode = null,
            ErrorMessage = null,
            LastCommandSucceeded = null,
        };
    }

    /// <summary>
    ///     Updates state when DepositFunds command fails.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action containing error details.</param>
    /// <returns>The new state with error populated.</returns>
    public static BankAccountAggregateState DepositFundsFailed(
        BankAccountAggregateState state,
        DepositFundsFailedAction action
    )
    {
        (ImmutableHashSet<string> inFlight, ImmutableList<CommandHistoryEntry> history) =
            AggregateCommandStateReducers.ComputeCommandFailed(state, action);
        return state with
        {
            InFlightCommands = inFlight,
            CommandHistory = history,
            LastCommandSucceeded = false,
            ErrorCode = action.ErrorCode,
            ErrorMessage = action.ErrorMessage,
        };
    }

    /// <summary>
    ///     Updates state when DepositFunds command succeeds.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action to reduce.</param>
    /// <returns>The new state with success set.</returns>
    public static BankAccountAggregateState DepositFundsSucceeded(
        BankAccountAggregateState state,
        DepositFundsSucceededAction action
    )
    {
        (ImmutableHashSet<string> inFlight, ImmutableList<CommandHistoryEntry> history) =
            AggregateCommandStateReducers.ComputeCommandSucceeded(state, action);
        return state with
        {
            InFlightCommands = inFlight,
            CommandHistory = history,
            LastCommandSucceeded = true,
            ErrorCode = null,
            ErrorMessage = null,
        };
    }
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
    )
    {
        (ImmutableHashSet<string> inFlight, ImmutableList<CommandHistoryEntry> history) =
            AggregateCommandStateReducers.ComputeCommandExecuting(state, action);
        return state with
        {
            InFlightCommands = inFlight,
            CommandHistory = history,
            ErrorCode = null,
            ErrorMessage = null,
            LastCommandSucceeded = null,
        };
    }

    /// <summary>
    ///     Updates state when OpenAccount command fails.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action containing error details.</param>
    /// <returns>The new state with error populated.</returns>
    public static BankAccountAggregateState OpenAccountFailed(
        BankAccountAggregateState state,
        OpenAccountFailedAction action
    )
    {
        (ImmutableHashSet<string> inFlight, ImmutableList<CommandHistoryEntry> history) =
            AggregateCommandStateReducers.ComputeCommandFailed(state, action);
        return state with
        {
            InFlightCommands = inFlight,
            CommandHistory = history,
            LastCommandSucceeded = false,
            ErrorCode = action.ErrorCode,
            ErrorMessage = action.ErrorMessage,
        };
    }

    /// <summary>
    ///     Updates state when OpenAccount command succeeds.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action to reduce.</param>
    /// <returns>The new state with success set.</returns>
    public static BankAccountAggregateState OpenAccountSucceeded(
        BankAccountAggregateState state,
        OpenAccountSucceededAction action
    )
    {
        (ImmutableHashSet<string> inFlight, ImmutableList<CommandHistoryEntry> history) =
            AggregateCommandStateReducers.ComputeCommandSucceeded(state, action);
        return state with
        {
            InFlightCommands = inFlight,
            CommandHistory = history,
            LastCommandSucceeded = true,
            ErrorCode = null,
            ErrorMessage = null,
        };
    }

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
    )
    {
        (ImmutableHashSet<string> inFlight, ImmutableList<CommandHistoryEntry> history) =
            AggregateCommandStateReducers.ComputeCommandExecuting(state, action);
        return state with
        {
            InFlightCommands = inFlight,
            CommandHistory = history,
            ErrorCode = null,
            ErrorMessage = null,
            LastCommandSucceeded = null,
        };
    }

    /// <summary>
    ///     Updates state when WithdrawFunds command fails.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action containing error details.</param>
    /// <returns>The new state with error populated.</returns>
    public static BankAccountAggregateState WithdrawFundsFailed(
        BankAccountAggregateState state,
        WithdrawFundsFailedAction action
    )
    {
        (ImmutableHashSet<string> inFlight, ImmutableList<CommandHistoryEntry> history) =
            AggregateCommandStateReducers.ComputeCommandFailed(state, action);
        return state with
        {
            InFlightCommands = inFlight,
            CommandHistory = history,
            LastCommandSucceeded = false,
            ErrorCode = action.ErrorCode,
            ErrorMessage = action.ErrorMessage,
        };
    }

    /// <summary>
    ///     Updates state when WithdrawFunds command succeeds.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action to reduce.</param>
    /// <returns>The new state with success set.</returns>
    public static BankAccountAggregateState WithdrawFundsSucceeded(
        BankAccountAggregateState state,
        WithdrawFundsSucceededAction action
    )
    {
        (ImmutableHashSet<string> inFlight, ImmutableList<CommandHistoryEntry> history) =
            AggregateCommandStateReducers.ComputeCommandSucceeded(state, action);
        return state with
        {
            InFlightCommands = inFlight,
            CommandHistory = history,
            LastCommandSucceeded = true,
            ErrorCode = null,
            ErrorMessage = null,
        };
    }
}