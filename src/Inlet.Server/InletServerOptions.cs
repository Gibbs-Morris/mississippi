namespace Mississippi.Inlet.Server;

/// <summary>
///     Configuration options for Inlet Server.
/// </summary>
public sealed class InletServerOptions
{
    /// <summary>
    ///     Gets or sets the Orleans stream namespace for hub-wide broadcasts.
    /// </summary>
    /// <value>Defaults to <c>"mississippi-all-clients"</c>.</value>
    public string AllClientsStreamNamespace { get; set; } = "mississippi-all-clients";

    /// <summary>
    ///     Gets or sets the interval in minutes between server heartbeats.
    /// </summary>
    /// <value>Defaults to 1 minute.</value>
    public int HeartbeatIntervalMinutes { get; set; } = 1;

    /// <summary>
    ///     Gets or sets the Orleans stream namespace for server-targeted messages.
    /// </summary>
    /// <value>Defaults to <c>"mississippi-server"</c>.</value>
    public string ServerStreamNamespace { get; set; } = "mississippi-server";

    /// <summary>
    ///     Gets or sets the name of the Orleans stream provider to use.
    /// </summary>
    /// <value>Defaults to <c>"mississippi-streaming"</c>.</value>
    public string StreamProviderName { get; set; } = "mississippi-streaming";
}