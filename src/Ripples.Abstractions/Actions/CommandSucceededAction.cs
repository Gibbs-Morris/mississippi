using System;


namespace Mississippi.Ripples.Abstractions.Actions;

/// <summary>
///     Action dispatched when a command has been successfully processed.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
public sealed record CommandSucceededAction<TCommand> : IAction
    where TCommand : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CommandSucceededAction{TCommand}" /> class.
    /// </summary>
    /// <param name="command">The command that succeeded.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="command" /> is null.</exception>
    public CommandSucceededAction(
        TCommand command
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        Command = command;
    }

    /// <summary>
    ///     Gets the command that succeeded.
    /// </summary>
    public TCommand Command { get; }

    /// <summary>
    ///     Gets the command type.
    /// </summary>
    public Type CommandType => typeof(TCommand);
}