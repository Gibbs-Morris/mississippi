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
    public static (ImmutableHashSet<string> InFlightCommands, ImmutableList<CommandHistoryEntry> CommandHistory) ComputeCommandExecuting(
        IAggregateCommandState state,
        ICommandExecutingAction action,
        int maxHistoryEntries = IAggregateCommandState.DefaultMaxHistoryEntries)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        var entry = CommandHistoryEntry.CreateExecuting(action.CommandId, action.CommandType, action.Timestamp);
        var inFlight = state.InFlightCommands.Add(action.CommandId);
        var history = EnforceHistoryLimit(state.CommandHistory.Add(entry), maxHistoryEntries);

        return (inFlight, history);
    }

    /// <summary>
    ///     Computes the updated in-flight commands and history for a command that has succeeded.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The succeeded action.</param>
    /// <returns>A tuple with the updated in-flight commands and command history.</returns>
    public static (ImmutableHashSet<string> InFlightCommands, ImmutableList<CommandHistoryEntry> CommandHistory) ComputeCommandSucceeded(
        IAggregateCommandState state,
        ICommandSucceededAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        var inFlight = state.InFlightCommands.Remove(action.CommandId);
        var history = UpdateHistoryEntry(state.CommandHistory, action.CommandId, entry => entry.ToSucceeded(action.Timestamp));

        return (inFlight, history);
    }

    /// <summary>
    ///     Computes the updated in-flight commands and history for a command that has failed.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The failed action.</param>
    /// <returns>A tuple with the updated in-flight commands and command history.</returns>
    public static (ImmutableHashSet<string> InFlightCommands, ImmutableList<CommandHistoryEntry> CommandHistory) ComputeCommandFailed(
        IAggregateCommandState state,
        ICommandFailedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        var inFlight = state.InFlightCommands.Remove(action.CommandId);
        var history = UpdateHistoryEntry(
            state.CommandHistory,
            action.CommandId,
            entry => entry.ToFailed(action.Timestamp, action.ErrorCode, action.ErrorMessage));

        return (inFlight, history);
    }

    private static ImmutableList<CommandHistoryEntry> EnforceHistoryLimit(
        ImmutableList<CommandHistoryEntry> history,
        int maxEntries)
    {
        while (history.Count > maxEntries)
        {
            history = history.RemoveAt(0);
        }

        return history;
    }

    private static ImmutableList<CommandHistoryEntry> UpdateHistoryEntry(
        ImmutableList<CommandHistoryEntry> history,
        string commandId,
        Func<CommandHistoryEntry, CommandHistoryEntry> transform)
    {
        var index = history.FindIndex(e => e.CommandId == commandId);
        if (index < 0)
        {
            return history;
        }

        return history.SetItem(index, transform(history[index]));
    }
}
