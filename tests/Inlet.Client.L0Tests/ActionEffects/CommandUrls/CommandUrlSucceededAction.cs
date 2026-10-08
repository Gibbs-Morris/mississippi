using System;

using Mississippi.Inlet.Client.Abstractions.Actions;


namespace Mississippi.Inlet.Client.L0Tests.ActionEffects.CommandUrls;

/// <summary>Records command success for URL tests.</summary>
/// <param name="CommandId">The invocation identifier.</param>
/// <param name="Timestamp">The completion time.</param>
internal sealed record CommandUrlSucceededAction(string CommandId, DateTimeOffset Timestamp)
    : ICommandSucceededAction<CommandUrlSucceededAction>
{
    /// <inheritdoc />
    public static CommandUrlSucceededAction Create(
        string commandId,
        DateTimeOffset timestamp
    ) =>
        new(commandId, timestamp);
}