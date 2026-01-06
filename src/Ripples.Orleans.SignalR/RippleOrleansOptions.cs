namespace Mississippi.Ripples.Orleans.SignalR;

/// <summary>
///     Configuration options for Ripples Orleans SignalR integration.
/// </summary>
public sealed class RippleOrleansOptions
{
    /// <summary>
    ///     Gets or sets the Orleans stream namespace for hub-wide broadcasts.
    /// </summary>
    /// <value>Defaults to "Ripple.AllClients".</value>
    public string AllClientsStreamNamespace { get; set; } = "Ripple.AllClients";

    /// <summary>
    ///     Gets or sets the interval in minutes between server heartbeats.
    /// </summary>
    /// <value>Defaults to 1 minute.</value>
    public int HeartbeatIntervalMinutes { get; set; } = 1;

    /// <summary>
    ///     Gets or sets the Orleans stream namespace for server-targeted messages.
    /// </summary>
    /// <value>Defaults to "Ripple.Server".</value>
    public string ServerStreamNamespace { get; set; } = "Ripple.Server";

    /// <summary>
    ///     Gets or sets the name of the Orleans stream provider to use.
    /// </summary>
    /// <value>Defaults to "BrookStreams".</value>
    public string StreamProviderName { get; set; } = "BrookStreams";
}