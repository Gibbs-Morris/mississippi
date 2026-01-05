using System;


namespace Mississippi.Ripples.Client;

/// <summary>
///     Configuration options for the Ripples client in Blazor WebAssembly.
/// </summary>
public sealed class RipplesClientOptions
{
    /// <summary>
    ///     Gets or sets the base URL for API requests.
    /// </summary>
    /// <remarks>
    ///     Should include the scheme and host, e.g., "https://api.example.com".
    ///     Do not include a trailing slash.
    /// </remarks>
    public Uri? BaseApiUri { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether automatic reconnection is enabled for SignalR.
    /// </summary>
    /// <remarks>
    ///     Default is true. When enabled, uses exponential backoff for reconnection attempts.
    /// </remarks>
    public bool EnableAutoReconnect { get; set; } = true;

    /// <summary>
    ///     Gets or sets the timeout for HTTP requests.
    /// </summary>
    /// <remarks>
    ///     Default is 30 seconds.
    /// </remarks>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Gets or sets the initial delay between reconnection attempts.
    /// </summary>
    /// <remarks>
    ///     Default is 1 second. Delay increases exponentially with each attempt.
    /// </remarks>
    public TimeSpan InitialReconnectDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     Gets or sets the maximum number of reconnection attempts.
    /// </summary>
    /// <remarks>
    ///     Default is 10. Set to 0 for unlimited attempts.
    /// </remarks>
    public int MaxReconnectAttempts { get; set; } = 10;

    /// <summary>
    ///     Gets or sets the maximum delay between reconnection attempts.
    /// </summary>
    /// <remarks>
    ///     Default is 30 seconds.
    /// </remarks>
    public TimeSpan MaxReconnectDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Gets or sets the path to the SignalR hub for projection updates.
    /// </summary>
    /// <remarks>
    ///     Default is "/hubs/projections".
    /// </remarks>
    public string SignalRHubPath { get; set; } = "/hubs/projections";
}