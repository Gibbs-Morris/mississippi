using System;

using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Client.Abstractions.Actions;

/// <summary>
///     Represents an action dispatched when a command fails.
/// </summary>
/// <remarks>
///     <para>
///         Each command type should have its own implementing action (e.g., <c>OpenAccountFailedAction</c>)
///         to enable per-command tracking, history, and correlation.
///     </para>
///     <para>
///         Implementations must provide a static factory method via the
///         <see cref="ICommandFailedAction{TSelf}" /> interface for factory creation.
///     </para>
/// </remarks>
public interface ICommandFailedAction : IAction
{
    /// <summary>
    ///     Gets the unique identifier for this command invocation.
    /// </summary>
    /// <remarks>
    ///     Correlates with the <see cref="ICommandExecutingAction.CommandId" /> from the initiating action.
    /// </remarks>
    string CommandId { get; }

    /// <summary>
    ///     Gets the error code describing the failure.
    /// </summary>
    string? ErrorCode { get; }

    /// <summary>
    ///     Gets a human-readable description of the error.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    ///     Gets the timestamp when the command failed.
    /// </summary>
    DateTimeOffset Timestamp { get; }
}

/// <summary>
///     Extends <see cref="ICommandFailedAction" /> with a static factory method for creating instances.
/// </summary>
/// <typeparam name="TSelf">The implementing type (CRTP pattern).</typeparam>
public interface ICommandFailedAction<TSelf> : ICommandFailedAction
    where TSelf : ICommandFailedAction<TSelf>
{
    /// <summary>
    ///     Creates a new instance of the failed action.
    /// </summary>
    /// <param name="commandId">The unique command invocation identifier.</param>
    /// <param name="errorCode">The error code describing the failure.</param>
    /// <param name="errorMessage">A human-readable description of the error.</param>
    /// <param name="timestamp">The timestamp when the command failed.</param>
    /// <returns>A new instance of the failed action.</returns>
    static abstract TSelf Create(
        string commandId,
        string? errorCode,
        string? errorMessage,
        DateTimeOffset timestamp
    );
}