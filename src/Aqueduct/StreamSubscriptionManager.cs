using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Messages;

using Orleans;
using Orleans.Runtime;
using Orleans.Streams;


namespace Mississippi.Aqueduct;

/// <summary>
///     Manages Orleans stream subscriptions for the Aqueduct SignalR backplane.
/// </summary>
/// <remarks>
///     <para>
///         This manager handles subscription to server-specific and hub-wide broadcast
///         streams. It routes incoming stream messages to the appropriate callbacks
///         provided during initialization.
///     </para>
/// </remarks>
internal sealed class StreamSubscriptionManager
    : IStreamSubscriptionManager,
      IDisposable
{
    private readonly SemaphoreSlim initLock = new(1);

    private IAsyncStream<AllMessage>? allStream;

    private bool disposed;

    private volatile bool initialized;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StreamSubscriptionManager" /> class.
    /// </summary>
    /// <param name="clusterClient">The Orleans cluster client for stream operations.</param>
    /// <param name="options">Configuration options for stream namespaces.</param>
    /// <param name="logger">Logger instance for stream operations.</param>
    public StreamSubscriptionManager(
        IClusterClient clusterClient,
        IOptions<AqueductOptions> options,
        ILogger<StreamSubscriptionManager> logger
    )
    {
        ClusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ServerId = Guid.NewGuid().ToString("N");
    }

    /// <inheritdoc />
    public bool IsInitialized => initialized;

    /// <inheritdoc />
    public string ServerId { get; }

    private IClusterClient ClusterClient { get; }

    private ILogger<StreamSubscriptionManager> Logger { get; }

    private IOptions<AqueductOptions> Options { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        initLock.Dispose();
    }

    /// <inheritdoc />
    public async Task EnsureInitializedAsync(
        string hubName,
        Func<ServerMessage, Task> onServerMessage,
        Func<AllMessage, Task> onAllMessage,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(hubName);
        ArgumentNullException.ThrowIfNull(onServerMessage);
        ArgumentNullException.ThrowIfNull(onAllMessage);
        if (initialized)
        {
            return;
        }

        await initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (initialized)
            {
                return;
            }

            Logger.InitializingStreams(hubName, ServerId);
            string streamProviderName = Options.Value.StreamProviderName;
            IStreamProvider streamProvider = ClusterClient.GetStreamProvider(streamProviderName);

            // Subscribe to server-specific stream
            StreamId serverStreamId = StreamId.Create(Options.Value.ServerStreamNamespace, ServerId);
            IAsyncStream<ServerMessage> serverStream = streamProvider.GetStream<ServerMessage>(serverStreamId);
            await serverStream.SubscribeAsync(async (
                    message,
                    token
                ) => await onServerMessage(message).ConfigureAwait(false))
                .ConfigureAwait(false);

            // Subscribe to hub broadcast stream
            StreamId allStreamId = StreamId.Create(Options.Value.AllClientsStreamNamespace, hubName);
            allStream = streamProvider.GetStream<AllMessage>(allStreamId);
            await allStream.SubscribeAsync(async (
                    message,
                    token
                ) => await onAllMessage(message).ConfigureAwait(false))
                .ConfigureAwait(false);
            initialized = true;
            Logger.StreamsInitialized(hubName, ServerId);
        }
        finally
        {
            initLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task PublishToAllAsync(
        AllMessage message
    )
    {
        ArgumentNullException.ThrowIfNull(message);
        if (allStream == null)
        {
            throw new InvalidOperationException("Stream not initialized. Call EnsureInitializedAsync first.");
        }

        await allStream.OnNextAsync(message).ConfigureAwait(false);
    }
}