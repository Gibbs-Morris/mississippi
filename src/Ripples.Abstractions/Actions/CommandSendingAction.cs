using System;


namespace Mississippi.Ripples.Abstractions.Actions;

/// <summary>
///     Action dispatched when a command is being sent to the server.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
public sealed record CommandSendingAction<TCommand> : IAction
    where TCommand : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CommandSendingAction{TCommand}" /> class.
    /// </summary>
    /// <param name="command">The command being sent.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="command" /> is null.</exception>
    public CommandSendingAction(
        TCommand command
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        Command = command;
    }

    /// <summary>
    ///     Gets the command being sent.
    /// </summary>
    public TCommand Command { get; }

    /// <summary>
    ///     Gets the command type.
    /// </summary>
    public Type CommandType => typeof(TCommand);
}