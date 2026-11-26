using System;

using Mississippi.Inlet.Client.Abstractions.Actions;


namespace MississippiTests.Inlet.Client.L0Tests.ActionEffects.Commands;

/// <summary>
///     Records the completion of a test command.
/// </summary>
/// <param name="CommandId">The invocation identifier.</param>
/// <param name="Timestamp">The completion time.</param>
internal sealed record CommandEffectSucceededAction(string CommandId, DateTimeOffset Timestamp)
    : ICommandSucceededAction<CommandEffectSucceededAction>
{
    /// <inheritdoc />
    public static CommandEffectSucceededAction Create(
        string commandId,
        DateTimeOffset timestamp
    ) =>
        new(commandId, timestamp);
}