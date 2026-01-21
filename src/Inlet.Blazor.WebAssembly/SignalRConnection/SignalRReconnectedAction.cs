using System;

using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Blazor.WebAssembly.SignalRConnection;

/// <summary>
///     Action dispatched when the SignalR connection has successfully reconnected.
/// </summary>
/// <remarks>
///     This action is dispatched after the automatic reconnect logic successfully
///     re-establishes the connection following an unexpected disconnection.
/// </remarks>
public sealed record SignalRReconnectedAction : IAction
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SignalRReconnectedAction" /> class.
    /// </summary>
    /// <param name="connectionId">The new connection identifier assigned by the server.</param>
    /// <param name="timestamp">The timestamp when the reconnection succeeded.</param>
    public SignalRReconnectedAction(
        string? connectionId,
        DateTimeOffset timestamp
    )
    {
        ConnectionId = connectionId;
        Timestamp = timestamp;
    }

    /// <summary>
    ///     Gets the new connection identifier assigned by the server.
    /// </summary>
    /// <remarks>
    ///     This may be null if the connection was configured to skip negotiation.
    ///     Note that this will be a different connection ID than before the disconnection.
    /// </remarks>
    public string? ConnectionId { get; }

    /// <summary>
    ///     Gets the timestamp when the reconnection succeeded.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}
