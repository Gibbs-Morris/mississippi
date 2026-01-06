using System.Collections.Generic;

using Orleans;


namespace Mississippi.EventSourcing.UxProjections.SignalR.Messages;

/// <summary>
///     Represents a broadcast message to all connections on a hub via the Orleans backplane.
/// </summary>
/// <remarks>
///     These messages are published to the all-clients stream for a hub
///     when broadcasting to every connected client.
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.UxProjections.SignalR.AllMessage")]
public sealed record AllMessage
{
    /// <summary>
    ///     Gets the arguments to pass to the hub method.
    /// </summary>
    [Id(1)]
    public IReadOnlyList<object?> Args { get; init; } = [];

    /// <summary>
    ///     Gets the optional list of connection IDs to exclude from the broadcast.
    /// </summary>
    [Id(2)]
    public IReadOnlyList<string>? ExcludedConnectionIds { get; init; }

    /// <summary>
    ///     Gets the name of the hub method to invoke.
    /// </summary>
    [Id(0)]
    public string MethodName { get; init; } = string.Empty;
}