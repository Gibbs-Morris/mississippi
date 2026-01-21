namespace Mississippi.Inlet.Blazor.WebAssembly.SignalRConnection;

/// <summary>
///     Represents the connection status of the SignalR hub connection.
/// </summary>
/// <remarks>
///     <para>
///         These statuses mirror the underlying <c>HubConnectionState</c> from SignalR
///         but are exposed as a dedicated enum for the Redux state management pattern.
///     </para>
/// </remarks>
public enum SignalRConnectionStatus
{
    /// <summary>
    ///     The connection is not established.
    /// </summary>
    Disconnected,

    /// <summary>
    ///     The connection is being established.
    /// </summary>
    Connecting,

    /// <summary>
    ///     The connection is established and active.
    /// </summary>
    Connected,

    /// <summary>
    ///     The connection was lost and is being re-established.
    /// </summary>
    Reconnecting,
}
