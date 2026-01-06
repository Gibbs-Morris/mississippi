namespace Mississippi.EventSourcing.UxProjections.SignalR;

/// <summary>
///     Configuration options for the Orleans SignalR backplane.
/// </summary>
public sealed class OrleansBackplaneOptions
{
    /// <summary>
    ///     Gets or sets the Orleans stream namespace for hub-wide broadcasts.
    /// </summary>
    /// <value>Defaults to "UxProjection.AllClients".</value>
    public string AllClientsStreamNamespace { get; set; } = "UxProjection.AllClients";

    /// <summary>
    ///     Gets or sets the interval in minutes between server heartbeats.
    /// </summary>
    /// <value>Defaults to 1 minute.</value>
    public int HeartbeatIntervalMinutes { get; set; } = 1;

    /// <summary>
    ///     Gets or sets the Orleans stream namespace for server-targeted messages.
    /// </summary>
    /// <value>Defaults to "UxProjection.Server".</value>
    public string ServerStreamNamespace { get; set; } = "UxProjection.Server";

    /// <summary>
    ///     Gets or sets the name of the Orleans stream provider to use.
    /// </summary>
    /// <value>Defaults to "BrookStreams".</value>
    public string StreamProviderName { get; set; } = "BrookStreams";
}