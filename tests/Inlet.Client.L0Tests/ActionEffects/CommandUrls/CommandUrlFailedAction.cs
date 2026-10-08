using System;

using Mississippi.Inlet.Client.Abstractions.Actions;


namespace Mississippi.Inlet.Client.L0Tests.ActionEffects.CommandUrls;

/// <summary>Records command failure for URL tests.</summary>
/// <param name="CommandId">The invocation identifier.</param>
/// <param name="ErrorCode">The failure code.</param>
/// <param name="ErrorMessage">The failure description.</param>
/// <param name="Timestamp">The completion time.</param>
internal sealed record CommandUrlFailedAction(
    string CommandId,
    string? ErrorCode,
    string? ErrorMessage,
    DateTimeOffset Timestamp
) : ICommandFailedAction<CommandUrlFailedAction>
{
    /// <inheritdoc />
    public static CommandUrlFailedAction Create(
        string commandId,
        string? errorCode,
        string? errorMessage,
        DateTimeOffset timestamp
    ) =>
        new(commandId, errorCode, errorMessage, timestamp);
}