using System.Collections.Generic;

using Orleans;


namespace Mississippi.EventSourcing.UxProjections.SignalR.Messages;

/// <summary>
///     Represents a message targeted at a specific SignalR connection via the Orleans backplane.
/// </summary>
/// <remarks>
///     These messages are published to server-specific Orleans streams
///     when a connection resides on a different server than the sender.
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.UxProjections.SignalR.ServerMessage")]
public sealed record ServerMessage
{
    /// <summary>
    ///     Gets the arguments to pass to the hub method.
    /// </summary>
    [Id(2)]
    public IReadOnlyList<object?> Args { get; init; } = [];

    /// <summary>
    ///     Gets the target connection identifier.
    /// </summary>
    [Id(0)]
    public string ConnectionId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the name of the hub method to invoke.
    /// </summary>
    [Id(1)]
    public string MethodName { get; init; } = string.Empty;
}