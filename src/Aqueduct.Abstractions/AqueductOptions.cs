namespace Mississippi.Aqueduct.Abstractions;

/// <summary>
///     Configuration options for the Aqueduct SignalR backplane.
/// </summary>
public sealed class AqueductOptions
{
    /// <summary>
    ///     Gets or sets the stream namespace for broadcasting to all clients.
    /// </summary>
    /// <value>
    ///     The all-clients stream namespace. Defaults to <c>"mississippi-all-clients"</c>.
    /// </value>
    public string AllClientsStreamNamespace { get; set; } = "mississippi-all-clients";

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
    ///     The server stream namespace. Defaults to <c>"mississippi-server"</c>.
    /// </value>
    public string ServerStreamNamespace { get; set; } = "mississippi-server";

    /// <summary>
    ///     Gets or sets the name of the Orleans stream provider to use for SignalR message delivery.
    /// </summary>
    /// <value>
    ///     The stream provider name. Defaults to <c>"mississippi-streaming"</c>.
    /// </value>
    public string StreamProviderName { get; set; } = "mississippi-streaming";
}