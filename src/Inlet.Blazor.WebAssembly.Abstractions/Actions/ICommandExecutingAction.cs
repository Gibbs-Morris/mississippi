using System;

using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Blazor.WebAssembly.Abstractions.Actions;

/// <summary>
///     Represents an action dispatched when a command starts executing.
/// </summary>
/// <remarks>
///     <para>
///         Each command type should have its own implementing action (e.g., <c>OpenAccountExecutingAction</c>)
///         to enable per-command tracking, history, and correlation.
///     </para>
///     <para>
///         Implementations must provide a static factory method via the
///         <see cref="ICommandExecutingAction{TSelf}" /> interface for factory creation.
///     </para>
/// </remarks>
public interface ICommandExecutingAction : IAction
{
    /// <summary>
    ///     Gets the unique identifier for this command invocation.
    /// </summary>
    /// <remarks>
    ///     Used to correlate executing, succeeded, and failed actions for the same command invocation.
    /// </remarks>
    string CommandId { get; }

    /// <summary>
    ///     Gets the name of the command type being executed.
    /// </summary>
    /// <example>"OpenAccount", "DepositFunds".</example>
    string CommandType { get; }

    /// <summary>
    ///     Gets the timestamp when the command started executing.
    /// </summary>
    DateTimeOffset Timestamp { get; }
}

/// <summary>
///     Extends <see cref="ICommandExecutingAction" /> with a static factory method for creating instances.
/// </summary>
/// <typeparam name="TSelf">The implementing type (CRTP pattern).</typeparam>
public interface ICommandExecutingAction<TSelf> : ICommandExecutingAction
    where TSelf : ICommandExecutingAction<TSelf>
{
    /// <summary>
    ///     Creates a new instance of the executing action.
    /// </summary>
    /// <param name="commandId">The unique command invocation identifier.</param>
    /// <param name="commandType">The name of the command type.</param>
    /// <param name="timestamp">The timestamp when the command started.</param>
    /// <returns>A new instance of the executing action.</returns>
    static abstract TSelf Create(string commandId, string commandType, DateTimeOffset timestamp);
}
