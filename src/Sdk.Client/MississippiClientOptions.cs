using System;


namespace Mississippi.Sdk.Client;

/// <summary>
///     Options for configuring Mississippi client applications.
/// </summary>
public sealed class MississippiClientOptions
{
    /// <summary>
    ///     Gets or sets a value indicating whether SignalR should auto-reconnect.
    /// </summary>
    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    ///     Gets or sets the base address used for API and SignalR calls.
    /// </summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>
    ///     Gets or sets the hub path prefix used for SignalR endpoints.
    /// </summary>
    public string HubPathPrefix { get; set; } = "/hubs";
}