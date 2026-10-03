using System;

using Mississippi.Inlet.Client.Abstractions.Actions;


namespace Mississippi.Inlet.Client.L0Tests.ActionEffects.CommandUrls;

/// <summary>Records command execution for URL tests.</summary>
/// <param name="CommandId">The invocation identifier.</param>
/// <param name="CommandType">The command type.</param>
/// <param name="Timestamp">The execution time.</param>
internal sealed record CommandUrlExecutingAction(string CommandId, string CommandType, DateTimeOffset Timestamp)
    : ICommandExecutingAction<CommandUrlExecutingAction>
{
    /// <inheritdoc />
    public static CommandUrlExecutingAction Create(
        string commandId,
        string commandType,
        DateTimeOffset timestamp
    ) =>
        new(commandId, commandType, timestamp);
}