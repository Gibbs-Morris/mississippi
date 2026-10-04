using System;

using Mississippi.Inlet.Client.Abstractions.Actions;


namespace Mississippi.Inlet.Client.L0Tests.Helpers;

/// <summary>
///     Test implementation of ICommandExecutingAction.
/// </summary>
internal sealed record TestCommandExecutingAction(string CommandId, string CommandType, DateTimeOffset Timestamp)
    : ICommandExecutingAction;