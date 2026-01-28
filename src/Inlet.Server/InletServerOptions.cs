using Mississippi.Common.Abstractions;


namespace Mississippi.Inlet.Server;

/// <summary>
///     Configuration options for Inlet Server.
/// </summary>
public sealed class InletServerOptions
{
    /// <summary>
    ///     Gets or sets the Orleans stream namespace for hub-wide broadcasts.
    /// </summary>
    /// <value>Defaults to <see cref="MississippiDefaults.StreamNamespaces.AllClients" />.</value>
    public string AllClientsStreamNamespace { get; set; } = MississippiDefaults.StreamNamespaces.AllClients;

    /// <summary>
    ///     Gets or sets the interval in minutes between server heartbeats.
    /// </summary>
    /// <value>Defaults to 1 minute.</value>
    public int HeartbeatIntervalMinutes { get; set; } = 1;

    /// <summary>
    ///     Gets or sets the Orleans stream namespace for server-targeted messages.
    /// </summary>
    /// <value>Defaults to <see cref="MississippiDefaults.StreamNamespaces.Server" />.</value>
    public string ServerStreamNamespace { get; set; } = MississippiDefaults.StreamNamespaces.Server;

    /// <summary>
    ///     Gets or sets the name of the Orleans stream provider to use.
    /// </summary>
    /// <value>Defaults to <see cref="MississippiDefaults.StreamProviderName" />.</value>
    public string StreamProviderName { get; set; } = MississippiDefaults.StreamProviderName;
}