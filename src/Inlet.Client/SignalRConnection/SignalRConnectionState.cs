using System;

using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Inlet.Client.SignalRConnection;

/// <summary>
///     Represents the state of the SignalR hub connection for Redux-style state management.
/// </summary>
/// <remarks>
///     <para>
///         This feature state tracks the transport-level SignalR connection status,
///         enabling UI components to react to connection lifecycle events such as
///         disconnections, reconnection attempts, and message activity.
///     </para>
///     <para>
///         Use cases include:
///         <list type="bullet">
///             <item>
///                 <description>Displaying connection status indicators</description>
///             </item>
///             <item>
///                 <description>Showing a panel when disconnected</description>
///             </item>
///             <item>
///                 <description>Animating on message receipt (heartbeat)</description>
///             </item>
///         </list>
///     </para>
/// </remarks>
public sealed record SignalRConnectionState : IFeatureState
{
    /// <summary>
    ///     Gets the unique key identifying this feature state in the store.
    /// </summary>
    public static string FeatureKey => "signalr-connection";

    /// <summary>
    ///     Gets the current connection identifier, if connected.
    /// </summary>
    public string? ConnectionId { get; init; }

    /// <summary>
    ///     Gets the timestamp when the connection was last successfully established.
    /// </summary>
    public DateTimeOffset? LastConnectedAt { get; init; }

    /// <summary>
    ///     Gets the timestamp when the connection was last disconnected.
    /// </summary>
    public DateTimeOffset? LastDisconnectedAt { get; init; }

    /// <summary>
    ///     Gets the last error message that occurred, if any.
    /// </summary>
    public string? LastError { get; init; }

    /// <summary>
    ///     Gets the timestamp when the last message was received from the server.
    /// </summary>
    /// <remarks>
    ///     This can be used to drive heartbeat animations or activity indicators.
    /// </remarks>
    public DateTimeOffset? LastMessageReceivedAt { get; init; }

    /// <summary>
    ///     Gets the current reconnect attempt count during a reconnection cycle.
    /// </summary>
    /// <remarks>
    ///     This resets to zero upon successful connection or reconnection.
    /// </remarks>
    public int ReconnectAttemptCount { get; init; }

    /// <summary>
    ///     Gets the current connection status.
    /// </summary>
    public SignalRConnectionStatus Status { get; init; } = SignalRConnectionStatus.Disconnected;
}