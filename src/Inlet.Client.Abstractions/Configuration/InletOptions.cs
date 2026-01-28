using System;


namespace Mississippi.Inlet.Client.Abstractions.Configuration;

/// <summary>
///     Options for configuring the Inlet store.
/// </summary>
public sealed class InletOptions
{
    /// <summary>
    ///     Gets or sets a value indicating whether to automatically reconnect on connection loss.
    /// </summary>
    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    ///     Gets or sets the default timeout for projection operations.
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Gets or sets the maximum number of reconnection attempts.
    /// </summary>
    public int MaxReconnectAttempts { get; set; } = 5;

    /// <summary>
    ///     Gets or sets the delay before reconnection attempts.
    /// </summary>
    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);
}