// <copyright file="OrleansSignalROptions.cs" company="Gibbs-Morris">
// Proprietary and Confidential.
// All rights reserved.
// </copyright>

namespace Mississippi.AspNetCore.SignalR.Orleans;

/// <summary>
///     Configuration options for the Orleans-SignalR bridge.
/// </summary>
public sealed class OrleansSignalROptions
{
    /// <summary>
    ///     Gets or sets the stream namespace for broadcasting to all clients.
    /// </summary>
    /// <value>
    ///     The all-clients stream namespace. Defaults to "SignalR.AllClients".
    /// </value>
    public string AllClientsStreamNamespace { get; set; } = "SignalR.AllClients";

    /// <summary>
    ///     Gets or sets the timeout multiplier for considering a server dead.
    /// </summary>
    /// <value>
    ///     The dead server timeout multiplier. Defaults to 3 (3x heartbeat interval).
    /// </value>
    public int DeadServerTimeoutMultiplier { get; set; } = 3;

    /// <summary>
    ///     Gets or sets the interval in minutes between server heartbeats.
    /// </summary>
    /// <value>
    ///     The heartbeat interval in minutes. Defaults to 1.
    /// </value>
    public int HeartbeatIntervalMinutes { get; set; } = 1;

    /// <summary>
    ///     Gets or sets the stream namespace for server-targeted messages.
    /// </summary>
    /// <value>
    ///     The server stream namespace. Defaults to "SignalR.Server".
    /// </value>
    public string ServerStreamNamespace { get; set; } = "SignalR.Server";

    /// <summary>
    ///     Gets or sets the name of the Orleans stream provider to use for SignalR message delivery.
    /// </summary>
    /// <value>
    ///     The stream provider name. Defaults to "SignalRStreams".
    /// </value>
    public string StreamProviderName { get; set; } = "SignalRStreams";
}