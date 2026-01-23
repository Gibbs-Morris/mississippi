using System;
using System.Collections.Immutable;

using Mississippi.Inlet.Blazor.WebAssembly.Abstractions.Actions;
using Mississippi.Inlet.Blazor.WebAssembly.Abstractions.Commands;
using Mississippi.Inlet.Blazor.WebAssembly.Abstractions.State;


namespace Mississippi.Inlet.Blazor.WebAssembly.Abstractions;

/// <summary>
///     Provides reducer helper methods for updating command tracking state.
/// </summary>
/// <remarks>
///     Use these methods in aggregate reducers to maintain <see cref="IAggregateCommandState" />
///     properties when handling command lifecycle actions.
/// </remarks>
public static class AggregateCommandStateReducers
{
    /// <summary>
    ///     Computes the updated in-flight commands and history for a command that has started executing.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The executing action.</param>
    /// <param name="maxHistoryEntries">Maximum history entries to retain (default: 200).</param>
    /// <returns>A tuple with the updated in-flight commands and command history.</returns>
    public static (ImmutableHashSet<string> InFlightCommands, ImmutableList<CommandHistoryEntry> CommandHistory)
        ComputeCommandExecuting(
            IAggregateCommandState state,
            ICommandExecutingAction action,
            int maxHistoryEntries = IAggregateCommandState.DefaultMaxHistoryEntries
        )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            action.CommandId,
            action.CommandType,
            action.Timestamp);
        ImmutableHashSet<string> inFlight = state.InFlightCommands.Add(action.CommandId);
        ImmutableList<CommandHistoryEntry> history = EnforceHistoryLimit(
            state.CommandHistory.Add(entry),
            maxHistoryEntries);
        return (inFlight, history);
    }

    /// <summary>
    ///     Computes the updated in-flight commands and history for a command that has failed.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The failed action.</param>
    /// <returns>A tuple with the updated in-flight commands and command history.</returns>
    public static (ImmutableHashSet<string> InFlightCommands, ImmutableList<CommandHistoryEntry> CommandHistory)
        ComputeCommandFailed(
            IAggregateCommandState state,
            ICommandFailedAction action
        )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        ImmutableHashSet<string> inFlight = state.InFlightCommands.Remove(action.CommandId);
        ImmutableList<CommandHistoryEntry> history = UpdateHistoryEntry(
            state.CommandHistory,
            action.CommandId,
            entry => entry.ToFailed(action.Timestamp, action.ErrorCode, action.ErrorMessage));
        return (inFlight, history);
    }

    /// <summary>
    ///     Computes the updated in-flight commands and history for a command that has succeeded.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The succeeded action.</param>
    /// <returns>A tuple with the updated in-flight commands and command history.</returns>
    public static (ImmutableHashSet<string> InFlightCommands, ImmutableList<CommandHistoryEntry> CommandHistory)
        ComputeCommandSucceeded(
            IAggregateCommandState state,
            ICommandSucceededAction action
        )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        ImmutableHashSet<string> inFlight = state.InFlightCommands.Remove(action.CommandId);
        ImmutableList<CommandHistoryEntry> history = UpdateHistoryEntry(
            state.CommandHistory,
            action.CommandId,
            entry => entry.ToSucceeded(action.Timestamp));
        return (inFlight, history);
    }

    /// <summary>
    ///     Reduces the state when a command starts executing.
    /// </summary>
    /// <typeparam name="TState">The concrete state type derived from <see cref="State.AggregateCommandStateBase" />.</typeparam>
    /// <param name="state">The current state.</param>
    /// <param name="action">The executing action.</param>
    /// <param name="maxHistoryEntries">Maximum history entries to retain.</param>
    /// <returns>The new state with command tracked and error state cleared.</returns>
    public static TState ReduceCommandExecuting<TState>(
        TState state,
        ICommandExecutingAction action,
        int maxHistoryEntries = IAggregateCommandState.DefaultMaxHistoryEntries
    )
        where TState : AggregateCommandStateBase
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            action.CommandId,
            action.CommandType,
            action.Timestamp);
        ImmutableHashSet<string> inFlight = state.InFlightCommands.Add(action.CommandId);
        ImmutableList<CommandHistoryEntry> history = EnforceHistoryLimit(
            state.CommandHistory.Add(entry),
            maxHistoryEntries);
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
    ///     Reduces the state when a command fails.
    /// </summary>
    /// <typeparam name="TState">The concrete state type derived from <see cref="State.AggregateCommandStateBase" />.</typeparam>
    /// <param name="state">The current state.</param>
    /// <param name="action">The failed action containing error details.</param>
    /// <returns>The new state with error populated.</returns>
    public static TState ReduceCommandFailed<TState>(
        TState state,
        ICommandFailedAction action
    )
        where TState : AggregateCommandStateBase
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        ImmutableHashSet<string> inFlight = state.InFlightCommands.Remove(action.CommandId);
        ImmutableList<CommandHistoryEntry> history = UpdateHistoryEntry(
            state.CommandHistory,
            action.CommandId,
            entry => entry.ToFailed(action.Timestamp, action.ErrorCode, action.ErrorMessage));
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
    ///     Reduces the state when a command succeeds.
    /// </summary>
    /// <typeparam name="TState">The concrete state type derived from <see cref="State.AggregateCommandStateBase" />.</typeparam>
    /// <param name="state">The current state.</param>
    /// <param name="action">The succeeded action.</param>
    /// <returns>The new state with success set and errors cleared.</returns>
    public static TState ReduceCommandSucceeded<TState>(
        TState state,
        ICommandSucceededAction action
    )
        where TState : AggregateCommandStateBase
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        ImmutableHashSet<string> inFlight = state.InFlightCommands.Remove(action.CommandId);
        ImmutableList<CommandHistoryEntry> history = UpdateHistoryEntry(
            state.CommandHistory,
            action.CommandId,
            entry => entry.ToSucceeded(action.Timestamp));
        return state with
        {
            InFlightCommands = inFlight,
            CommandHistory = history,
            LastCommandSucceeded = true,
            ErrorCode = null,
            ErrorMessage = null,
        };
    }

    private static ImmutableList<CommandHistoryEntry> EnforceHistoryLimit(
        ImmutableList<CommandHistoryEntry> history,
        int maxEntries
    )
    {
        if (history.Count <= maxEntries)
        {
            return history;
        }

        // Remove oldest entries in a single operation to avoid intermediate allocations
        int removeCount = history.Count - maxEntries;
        return history.RemoveRange(0, removeCount);
    }

    private static ImmutableList<CommandHistoryEntry> UpdateHistoryEntry(
        ImmutableList<CommandHistoryEntry> history,
        string commandId,
        Func<CommandHistoryEntry, CommandHistoryEntry> transform
    )
    {
        int index = history.FindIndex(e => e.CommandId == commandId);
        if (index < 0)
        {
            return history;
        }

        return history.SetItem(index, transform(history[index]));
    }
}