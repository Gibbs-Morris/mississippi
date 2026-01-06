using System;

using Orleans;


namespace Mississippi.Viaduct.Grains.State;

/// <summary>
///     Persisted state for a client grain implementing <see cref="ISignalRClientGrain" />.
/// </summary>
/// <remarks>
///     Tracks connection metadata including the hub name, hosting server,
///     and connection timestamp.
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.Viaduct.SignalRClientState")]
public sealed record SignalRClientState
{
    /// <summary>
    ///     Gets the timestamp when the connection was established.
    /// </summary>
    [Id(3)]
    public DateTimeOffset ConnectedAt { get; init; }

    /// <summary>
    ///     Gets the SignalR connection identifier.
    /// </summary>
    [Id(0)]
    public string ConnectionId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the name of the SignalR hub.
    /// </summary>
    [Id(1)]
    public string HubName { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the identifier of the server hosting this connection.
    /// </summary>
    [Id(2)]
    public string ServerId { get; init; } = string.Empty;
}