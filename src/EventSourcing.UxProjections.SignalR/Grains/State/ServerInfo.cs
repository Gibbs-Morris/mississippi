using System;

using Orleans;


namespace Mississippi.EventSourcing.UxProjections.SignalR.Grains.State;

/// <summary>
///     Information about a SignalR server registered in the directory.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.UxProjections.SignalR.ServerInfo")]
public sealed record ServerInfo
{
    /// <summary>
    ///     Gets the number of active connections on this server.
    /// </summary>
    [Id(2)]
    public int ConnectionCount { get; init; }

    /// <summary>
    ///     Gets the timestamp of the last heartbeat received from this server.
    /// </summary>
    [Id(1)]
    public DateTimeOffset LastHeartbeat { get; init; }

    /// <summary>
    ///     Gets the unique identifier of the server.
    /// </summary>
    [Id(0)]
    public string ServerId { get; init; } = string.Empty;
}