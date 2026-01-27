using System;

using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Client.Abstractions.Actions;

/// <summary>
///     Represents an action dispatched when a command completes successfully.
/// </summary>
/// <remarks>
///     <para>
///         Each command type should have its own implementing action (e.g., <c>OpenAccountSucceededAction</c>)
///         to enable per-command tracking, history, and correlation.
///     </para>
///     <para>
///         Implementations must provide a static factory method via the
///         <see cref="ICommandSucceededAction{TSelf}" /> interface for factory creation.
///     </para>
/// </remarks>
public interface ICommandSucceededAction : IAction
{
    /// <summary>
    ///     Gets the unique identifier for this command invocation.
    /// </summary>
    /// <remarks>
    ///     Correlates with the <see cref="ICommandExecutingAction.CommandId" /> from the initiating action.
    /// </remarks>
    string CommandId { get; }

    /// <summary>
    ///     Gets the timestamp when the command completed successfully.
    /// </summary>
    DateTimeOffset Timestamp { get; }
}

/// <summary>
///     Extends <see cref="ICommandSucceededAction" /> with a static factory method for creating instances.
/// </summary>
/// <typeparam name="TSelf">The implementing type (CRTP pattern).</typeparam>
public interface ICommandSucceededAction<TSelf> : ICommandSucceededAction
    where TSelf : ICommandSucceededAction<TSelf>
{
    /// <summary>
    ///     Creates a new instance of the succeeded action.
    /// </summary>
    /// <param name="commandId">The unique command invocation identifier.</param>
    /// <param name="timestamp">The timestamp when the command completed.</param>
    /// <returns>A new instance of the succeeded action.</returns>
    static abstract TSelf Create(
        string commandId,
        DateTimeOffset timestamp
    );
}