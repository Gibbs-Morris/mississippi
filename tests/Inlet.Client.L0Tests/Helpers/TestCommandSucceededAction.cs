using System;

using Mississippi.Inlet.Client.Abstractions.Actions;


namespace Mississippi.Inlet.Client.L0Tests;

/// <summary>
///     Test implementation of ICommandSucceededAction.
/// </summary>
internal sealed record TestCommandSucceededAction(
    string CommandId,
    DateTimeOffset Timestamp
) : ICommandSucceededAction;
