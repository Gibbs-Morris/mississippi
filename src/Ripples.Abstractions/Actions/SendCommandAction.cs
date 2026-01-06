using System;


namespace Mississippi.Ripples.Abstractions.Actions;

/// <summary>
///     Action to send a command to an aggregate grain.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
public sealed record SendCommandAction<TCommand> : IAction
    where TCommand : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SendCommandAction{TCommand}" /> class.
    /// </summary>
    /// <param name="command">The command to send.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="command" /> is null.</exception>
    public SendCommandAction(
        TCommand command
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        Command = command;
    }

    /// <summary>
    ///     Gets the command to send.
    /// </summary>
    public TCommand Command { get; }

    /// <summary>
    ///     Gets the command type.
    /// </summary>
    public Type CommandType => typeof(TCommand);
}