using System;


namespace Mississippi.Inlet.Client.Abstractions.Commands;

/// <summary>
///     Represents a historical record of a command execution.
/// </summary>
/// <param name="CommandId">The unique identifier for this command invocation.</param>
/// <param name="CommandType">The name of the command type.</param>
/// <param name="Status">The current status of the command.</param>
/// <param name="StartedAt">The timestamp when the command started executing.</param>
/// <param name="CompletedAt">
///     The timestamp when the command completed (succeeded or failed), or <c>null</c> if still
///     executing.
/// </param>
/// <param name="ErrorCode">The error code if the command failed, or <c>null</c> otherwise.</param>
/// <param name="ErrorMessage">The error message if the command failed, or <c>null</c> otherwise.</param>
public sealed record CommandHistoryEntry(
    string CommandId,
    string CommandType,
    CommandStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string? ErrorCode,
    string? ErrorMessage
)
{
    /// <summary>
    ///     Creates a new history entry for a command that is executing.
    /// </summary>
    /// <param name="commandId">The unique command invocation identifier.</param>
    /// <param name="commandType">The name of the command type.</param>
    /// <param name="startedAt">The timestamp when the command started.</param>
    /// <returns>A new history entry with <see cref="CommandStatus.Executing" /> status.</returns>
    public static CommandHistoryEntry CreateExecuting(
        string commandId,
        string commandType,
        DateTimeOffset startedAt
    ) =>
        new(commandId, commandType, CommandStatus.Executing, startedAt, null, null, null);

    /// <summary>
    ///     Creates a new history entry representing a failed state from an existing entry.
    /// </summary>
    /// <param name="completedAt">The timestamp when the command failed.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A new history entry with <see cref="CommandStatus.Failed" /> status.</returns>
    public CommandHistoryEntry ToFailed(
        DateTimeOffset completedAt,
        string? errorCode,
        string? errorMessage
    ) =>
        this with
        {
            Status = CommandStatus.Failed,
            CompletedAt = completedAt,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
        };

    /// <summary>
    ///     Creates a new history entry representing a succeeded state from an existing entry.
    /// </summary>
    /// <param name="completedAt">The timestamp when the command completed.</param>
    /// <returns>A new history entry with <see cref="CommandStatus.Succeeded" /> status.</returns>
    public CommandHistoryEntry ToSucceeded(
        DateTimeOffset completedAt
    ) =>
        this with
        {
            Status = CommandStatus.Succeeded,
            CompletedAt = completedAt,
        };
}