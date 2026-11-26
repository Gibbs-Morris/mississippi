using System;

using Mississippi.Inlet.Client.Abstractions.Actions;


namespace MississippiTests.Inlet.Client.L0Tests.ActionEffects.Commands;

/// <summary>
///     Records the start of a test command.
/// </summary>
/// <param name="CommandId">The invocation identifier.</param>
/// <param name="CommandType">The command type.</param>
/// <param name="Timestamp">The start time.</param>
internal sealed record CommandEffectExecutingAction(string CommandId, string CommandType, DateTimeOffset Timestamp)
    : ICommandExecutingAction<CommandEffectExecutingAction>
{
    /// <inheritdoc />
    public static CommandEffectExecutingAction Create(
        string commandId,
        string commandType,
        DateTimeOffset timestamp
    ) =>
        new(commandId, commandType, timestamp);
}