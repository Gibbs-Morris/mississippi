using Mississippi.Common.Abstractions.Attributes;

using Spring.Client.Features.BankAccountAggregate.Actions;
using Spring.Client.Features.BankAccountAggregate.State;


namespace Spring.Client.Features.BankAccountAggregate.Reducers;

/// <summary>
///     Pure reducer functions for the BankAccountAggregate feature state.
/// </summary>
[PendingSourceGenerator]
internal static class BankAccountAggregateReducers
{
    /// <summary>
    ///     Sets executing state when a command starts.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action to reduce.</param>
    /// <returns>The new state with executing set to true.</returns>
    public static BankAccountAggregateState CommandExecuting(
        BankAccountAggregateState state,
        CommandExecutingAction action
    ) =>
        state with
        {
            IsExecuting = true,
            ErrorCode = null,
            ErrorMessage = null,
            LastCommandSucceeded = null,
        };

    /// <summary>
    ///     Sets error state when a command fails.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action containing the error details.</param>
    /// <returns>The new state with the error populated.</returns>
    public static BankAccountAggregateState CommandFailed(
        BankAccountAggregateState state,
        CommandFailedAction action
    ) =>
        state with
        {
            IsExecuting = false,
            LastCommandSucceeded = false,
            ErrorCode = action.ErrorCode,
            ErrorMessage = action.ErrorMessage,
        };

    /// <summary>
    ///     Sets success state when a command completes.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action to reduce.</param>
    /// <returns>The new state with success set to true.</returns>
    public static BankAccountAggregateState CommandSucceeded(
        BankAccountAggregateState state,
        CommandSucceededAction action
    ) =>
        state with
        {
            IsExecuting = false,
            LastCommandSucceeded = true,
            ErrorCode = null,
            ErrorMessage = null,
        };

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
}