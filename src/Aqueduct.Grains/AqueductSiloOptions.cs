using System;

using Orleans.Hosting;


namespace Mississippi.Aqueduct.Grains;

/// <summary>
///     Options for configuring Aqueduct on an Orleans silo.
/// </summary>
/// <remarks>
///     <para>
///         This options class provides a fluent API for configuring the Aqueduct
///         SignalR backplane on Orleans silos. It wraps <see cref="ISiloBuilder" />
///         to allow convenient configuration of stream providers.
///     </para>
/// </remarks>
public sealed class AqueductSiloOptions
{
    private readonly ISiloBuilder siloBuilder;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AqueductSiloOptions" /> class.
    /// </summary>
    /// <param name="siloBuilder">The silo builder to configure.</param>
    internal AqueductSiloOptions(
        ISiloBuilder siloBuilder
    )
    {
        this.siloBuilder = siloBuilder ?? throw new ArgumentNullException(nameof(siloBuilder));
    }

    /// <summary>
    ///     Gets or sets the stream namespace for broadcasting to all clients.
    /// </summary>
    /// <value>
    ///     The all-clients stream namespace. Defaults to "SignalR.AllClients".
    /// </value>
    public string AllClientsStreamNamespace { get; set; } = "SignalR.AllClients";

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
    ///     The server stream namespace. Defaults to "SignalR.Server".
    /// </value>
    public string ServerStreamNamespace { get; set; } = "SignalR.Server";

    /// <summary>
    ///     Gets or sets the name of the Orleans stream provider to use for SignalR message delivery.
    /// </summary>
    /// <value>
    ///     The stream provider name. Defaults to "SignalRStreams".
    /// </value>
    public string StreamProviderName { get; set; } = "SignalRStreams";

    /// <summary>
    ///     Configures in-memory streams for development and testing scenarios.
    /// </summary>
    /// <returns>The options instance for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method configures Orleans memory streams with the default stream provider
    ///         name ("SignalRStreams") and PubSub store ("PubSubStore"). Use this for local
    ///         development and testing.
    ///     </para>
    ///     <para>
    ///         For production scenarios, use a persistent stream provider like Azure Event Hubs
    ///         or Apache Kafka.
    ///     </para>
    /// </remarks>
    public AqueductSiloOptions UseMemoryStreams()
    {
        return UseMemoryStreams(StreamProviderName, "PubSubStore");
    }

    /// <summary>
    ///     Configures in-memory streams with custom names for development and testing scenarios.
    /// </summary>
    /// <param name="streamProviderName">The name of the stream provider.</param>
    /// <param name="pubSubStoreName">The name of the PubSub grain storage.</param>
    /// <returns>The options instance for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method configures Orleans memory streams with custom provider and storage names.
    ///         The <paramref name="streamProviderName" /> will be used as the
    ///         <see cref="StreamProviderName" /> for Aqueduct.
    ///     </para>
    /// </remarks>
    public AqueductSiloOptions UseMemoryStreams(
        string streamProviderName,
        string pubSubStoreName
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(streamProviderName);
        ArgumentException.ThrowIfNullOrEmpty(pubSubStoreName);

        StreamProviderName = streamProviderName;
        siloBuilder.AddMemoryStreams(streamProviderName);
        siloBuilder.AddMemoryGrainStorage(pubSubStoreName);

        return this;
    }
}
