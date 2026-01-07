namespace Mississippi.Inlet.Orleans.SignalR;

/// <summary>
///     Configuration options for Inlet Orleans SignalR integration.
/// </summary>
public sealed class InletOrleansOptions
{
    /// <summary>
    ///     Gets or sets the Orleans stream namespace for hub-wide broadcasts.
    /// </summary>
    /// <value>Defaults to "Inlet.AllClients".</value>
    public string AllClientsStreamNamespace { get; set; } = "Inlet.AllClients";

    /// <summary>
    ///     Gets or sets the interval in minutes between server heartbeats.
    /// </summary>
    /// <value>Defaults to 1 minute.</value>
    public int HeartbeatIntervalMinutes { get; set; } = 1;

    /// <summary>
    ///     Gets or sets the Orleans stream namespace for server-targeted messages.
    /// </summary>
    /// <value>Defaults to "Inlet.Server".</value>
    public string ServerStreamNamespace { get; set; } = "Inlet.Server";

    /// <summary>
    ///     Gets or sets the name of the Orleans stream provider to use.
    /// </summary>
    /// <value>Defaults to "BrookStreams".</value>
    public string StreamProviderName { get; set; } = "BrookStreams";
}