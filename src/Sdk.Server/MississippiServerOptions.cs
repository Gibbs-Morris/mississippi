namespace Mississippi.Sdk.Server;

/// <summary>
///     Options for configuring Mississippi server applications.
/// </summary>
public sealed class MississippiServerOptions
{
    /// <summary>
    ///     Gets or sets the API route prefix.
    /// </summary>
    public string ApiPrefix { get; set; } = "/api";

    /// <summary>
    ///     Gets or sets a value indicating whether CORS should be enabled for WASM clients.
    /// </summary>
    public bool EnableCors { get; set; } = true;

    /// <summary>
    ///     Gets or sets the hub path prefix used for SignalR endpoints.
    /// </summary>
    public string HubPathPrefix { get; set; } = "/hubs";
}