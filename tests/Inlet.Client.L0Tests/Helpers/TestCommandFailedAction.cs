using System;

using Mississippi.Inlet.Client.Abstractions.Actions;


namespace Mississippi.Inlet.Client.L0Tests;

/// <summary>
///     Test implementation of ICommandFailedAction.
/// </summary>
internal sealed record TestCommandFailedAction(
    string CommandId,
    DateTimeOffset Timestamp,
    string? ErrorCode,
    string? ErrorMessage
) : ICommandFailedAction;
