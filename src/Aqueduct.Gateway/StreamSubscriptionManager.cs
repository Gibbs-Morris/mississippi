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


namespace Mississippi.Aqueduct.Gateway;

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

    private Task<StreamSubscriptionHandle<AllMessage>>? allSubscriptionTask;

    private bool disposed;

    private volatile bool initialized;

    private Task<StreamSubscriptionHandle<ServerMessage>>? serverSubscriptionTask;

    private Task<bool>? subscriptionCleanupTask;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StreamSubscriptionManager" /> class.
    /// </summary>
    /// <param name="serverIdProvider">The provider for the server's unique identifier.</param>
    /// <param name="clusterClient">The Orleans cluster client for stream operations.</param>
    /// <param name="options">Configuration options for stream namespaces.</param>
    /// <param name="logger">Logger instance for stream operations.</param>
    public StreamSubscriptionManager(
        IServerIdProvider serverIdProvider,
        IClusterClient clusterClient,
        IOptions<AqueductOptions> options,
        ILogger<StreamSubscriptionManager> logger
    )
    {
        ArgumentNullException.ThrowIfNull(serverIdProvider);
        ClusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ServerId = serverIdProvider.ServerId;
    }

    /// <inheritdoc />
    public bool IsInitialized => initialized;

    /// <summary>
    ///     Gets the unique identifier for this server instance.
    /// </summary>
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

            if (subscriptionCleanupTask is not null)
            {
                bool cleanedUp = await subscriptionCleanupTask.WaitAsync(cancellationToken).ConfigureAwait(false);
                if (!cleanedUp)
                {
                    // Retain failed handles and retry cleanup before allowing any new subscription.
                    subscriptionCleanupTask = CleanupSubscriptionsAsync();
                    cleanedUp = await subscriptionCleanupTask.WaitAsync(cancellationToken).ConfigureAwait(false);
                    if (!cleanedUp)
                    {
                        throw new InvalidOperationException(
                            "Previous Orleans stream subscriptions could not be cleaned up.");
                    }
                }

                subscriptionCleanupTask = null;
            }

            Logger.InitializingStreams(hubName, ServerId);
            string streamProviderName = Options.Value.StreamProviderName;
            IStreamProvider streamProvider = ClusterClient.GetStreamProvider(streamProviderName);

            // Subscribe to server-specific stream
            StreamId serverStreamId = StreamId.Create(Options.Value.ServerStreamNamespace, ServerId);
            IAsyncStream<ServerMessage> serverStream = streamProvider.GetStream<ServerMessage>(serverStreamId);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                serverSubscriptionTask = serverStream.SubscribeAsync(async (
                    message,
                    token
                ) => await onServerMessage(message).ConfigureAwait(false));
                await serverSubscriptionTask.WaitAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                // Subscribe to hub broadcast stream
                StreamId allStreamId = StreamId.Create(Options.Value.AllClientsStreamNamespace, hubName);
                IAsyncStream<AllMessage> broadcastStream = streamProvider.GetStream<AllMessage>(allStreamId);
                allSubscriptionTask = broadcastStream.SubscribeAsync(async (
                    message,
                    token
                ) => await onAllMessage(message).ConfigureAwait(false));
                await allSubscriptionTask.WaitAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                allStream = broadcastStream;
                initialized = true;
                Logger.StreamsInitialized(hubName, ServerId);
            }
            catch (Exception exception)
            {
                Logger.StreamInitializationFailed(hubName, ServerId, exception);

                // Orleans subscriptions cannot be canceled. Observe late handles and release them,
                // including partial setup after a fault, while allowing the caller to stop waiting.
                subscriptionCleanupTask = CleanupSubscriptionsAsync();
                throw;
            }
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

    private async Task<bool> CleanupSubscriptionAsync<T>(
        Task<StreamSubscriptionHandle<T>>? subscriptionTask
    )
    {
        if (subscriptionTask is null)
        {
            return true;
        }

        StreamSubscriptionHandle<T> subscription;
        try
        {
            subscription = await subscriptionTask.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            Logger.SubscriptionCleanupCanceled(ServerId, exception);
            return true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // Subscription creation failed, so there is no handle to release.
            Logger.SubscriptionCleanupFailed(ServerId, exception);
            return true;
        }

        try
        {
            await subscription.UnsubscribeAsync().ConfigureAwait(false);
            return true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Logger.SubscriptionCleanupFailed(ServerId, exception);
            return false;
        }
    }

    private async Task<bool> CleanupSubscriptionsAsync()
    {
        Task<bool> serverCleanup = CleanupSubscriptionAsync(serverSubscriptionTask);
        Task<bool> allCleanup = CleanupSubscriptionAsync(allSubscriptionTask);
        await Task.WhenAll(serverCleanup, allCleanup).ConfigureAwait(false);
        if (await serverCleanup.ConfigureAwait(false))
        {
            serverSubscriptionTask = null;
        }

        if (await allCleanup.ConfigureAwait(false))
        {
            allSubscriptionTask = null;
        }

        return serverSubscriptionTask is null && allSubscriptionTask is null;
    }
}