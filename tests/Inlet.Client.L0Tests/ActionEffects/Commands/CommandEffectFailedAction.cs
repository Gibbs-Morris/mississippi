using System;

using Mississippi.Inlet.Client.Abstractions.Actions;


namespace MississippiTests.Inlet.Client.L0Tests.ActionEffects.Commands;

/// <summary>
///     Records the failure of a test command.
/// </summary>
/// <param name="CommandId">The invocation identifier.</param>
/// <param name="ErrorCode">The failure category.</param>
/// <param name="ErrorMessage">The failure description.</param>
/// <param name="Timestamp">The failure time.</param>
internal sealed record CommandEffectFailedAction(
    string CommandId,
    string? ErrorCode,
    string? ErrorMessage,
    DateTimeOffset Timestamp
) : ICommandFailedAction<CommandEffectFailedAction>
{
    /// <inheritdoc />
    public static CommandEffectFailedAction Create(
        string commandId,
        string? errorCode,
        string? errorMessage,
        DateTimeOffset timestamp
    ) =>
        new(commandId, errorCode, errorMessage, timestamp);
}