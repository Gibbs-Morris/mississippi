using System;

using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Blazor.WebAssembly.SignalRConnection;

/// <summary>
///     Action dispatched when the SignalR connection is successfully established.
/// </summary>
public sealed record SignalRConnectedAction : IAction
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SignalRConnectedAction" /> class.
    /// </summary>
    /// <param name="connectionId">The connection identifier assigned by the server.</param>
    /// <param name="timestamp">The timestamp when the connection was established.</param>
    public SignalRConnectedAction(
        string? connectionId,
        DateTimeOffset timestamp
    )
    {
        ConnectionId = connectionId;
        Timestamp = timestamp;
    }

    /// <summary>
    ///     Gets the connection identifier assigned by the server.
    /// </summary>
    /// <remarks>
    ///     This may be null if the connection was configured to skip negotiation.
    /// </remarks>
    public string? ConnectionId { get; }

    /// <summary>
    ///     Gets the timestamp when the connection was established.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}
