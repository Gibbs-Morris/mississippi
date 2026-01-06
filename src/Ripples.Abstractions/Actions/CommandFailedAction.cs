using System;


namespace Mississippi.Ripples.Abstractions.Actions;

/// <summary>
///     Action dispatched when a command fails to process.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
public sealed record CommandFailedAction<TCommand> : IAction
    where TCommand : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CommandFailedAction{TCommand}" /> class.
    /// </summary>
    /// <param name="command">The command that failed.</param>
    /// <param name="error">The error that occurred.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public CommandFailedAction(
        TCommand command,
        Exception error
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(error);
        Command = command;
        Error = error;
    }

    /// <summary>
    ///     Gets the command that failed.
    /// </summary>
    public TCommand Command { get; }

    /// <summary>
    ///     Gets the command type.
    /// </summary>
    public Type CommandType => typeof(TCommand);

    /// <summary>
    ///     Gets the error that occurred.
    /// </summary>
    public Exception Error { get; }
}