using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

using Mississippi.Ripples.Abstractions;


namespace Cascade.WebApi.Client.Services;

/// <summary>
///     A WASM-compatible projection cache that uses HTTP to fetch projections
///     and SignalR for real-time updates.
/// </summary>
/// <remarks>
///     <para>
///         This implementation is designed for Blazor WebAssembly clients that cannot
///         use the full Ripples.Client library due to ASP.NET Core framework references.
///         It provides the same <see cref="IProjectionCache" /> interface for component-level
///         compatibility.
///     </para>
/// </remarks>
internal sealed class WasmProjectionCache : IProjectionCache
{
    private readonly ConcurrentDictionary<string, object> cache = new();

    private readonly SemaphoreSlim connectionLock = new(1, 1);

    private readonly HubConnection hubConnection;

    private readonly IDisposable projectionUpdatedSubscription;

    private readonly ConcurrentDictionary<string, SubscriptionGroup> subscriptions = new();

    private bool isDisposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="WasmProjectionCache" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for fetching projections.</param>
    /// <param name="logger">The logger.</param>
    public WasmProjectionCache(
        HttpClient httpClient,
        ILogger<WasmProjectionCache> logger
    )
    {
        HttpClient = httpClient;
        Logger = logger;
        string baseUrl = httpClient.BaseAddress?.AbsoluteUri.TrimEnd('/') ??
                         throw new InvalidOperationException("HttpClient.BaseAddress must be configured.");
        string hubUrl = $"{baseUrl}/hubs/uxprojections";
        hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();
        hubConnection.Reconnected += OnReconnectedAsync;
        projectionUpdatedSubscription = hubConnection.On<string, string, long>(
            RippleHubConstants.ProjectionUpdatedMethod,
            OnProjectionUpdatedAsync);
    }

    private HttpClient HttpClient { get; }

    private ILogger<WasmProjectionCache> Logger { get; }

    /// <inheritdoc />
    public IProjectionBinder<TProjection> CreateBinder<TProjection>()
        where TProjection : class =>
        new WasmProjectionBinder<TProjection>(this);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        subscriptions.Clear();
        cache.Clear();
        projectionUpdatedSubscription.Dispose();
        hubConnection.Reconnected -= OnReconnectedAsync;
        await hubConnection.DisposeAsync().ConfigureAwait(false);
        connectionLock.Dispose();
    }

    /// <inheritdoc />
    public Task EvictAsync<TProjection>(
        string entityId
    )
        where TProjection : class
    {
        string key = CreateCacheKey<TProjection>(entityId);
        cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public TProjection? GetCached<TProjection>(
        string entityId
    )
        where TProjection : class
    {
        string key = CreateCacheKey<TProjection>(entityId);
        if (cache.TryGetValue(key, out object? value) && (value is CacheEntry<TProjection> entry))
        {
            return entry.Data;
        }

        return null;
    }

    /// <inheritdoc />
    public bool IsCached<TProjection>(
        string entityId
    )
        where TProjection : class
    {
        string key = CreateCacheKey<TProjection>(entityId);
        return cache.ContainsKey(key);
    }

    /// <inheritdoc />
    public async Task RefreshAsync<TProjection>(
        string entityId,
        CancellationToken cancellationToken = default
    )
        where TProjection : class
    {
        string projectionType = typeof(TProjection).Name;
        TProjection? data = await FetchProjectionAsync<TProjection>(entityId, cancellationToken)
            .ConfigureAwait(false);
        if (data != null)
        {
            string key = CreateCacheKey<TProjection>(entityId);
            cache.AddOrUpdate(
                key,
                _ => new CacheEntry<TProjection>(data),
                (_, existing) =>
                {
                    if (existing is CacheEntry<TProjection> typedEntry)
                    {
                        typedEntry.Data = data;
                        return typedEntry;
                    }

                    return new CacheEntry<TProjection>(data);
                });

            // Notify subscribers
            string subscriptionKey = CreateSubscriptionKey(projectionType, entityId);
            if (subscriptions.TryGetValue(subscriptionKey, out SubscriptionGroup? group))
            {
                group.NotifyAll();
            }
        }
    }

    /// <inheritdoc />
    public async Task<IProjectionSubscription<TProjection>> SubscribeAsync<TProjection>(
        string entityId,
        Action onChanged,
        CancellationToken cancellationToken = default
    )
        where TProjection : class
    {
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        string projectionType = typeof(TProjection).Name;
        string subscriptionKey = CreateSubscriptionKey(projectionType, entityId);
        SubscriptionGroup group = subscriptions.GetOrAdd(
            subscriptionKey,
            _ => new SubscriptionGroup(projectionType, entityId));

        // Subscribe on the hub
        if (hubConnection.State == HubConnectionState.Connected)
        {
            await hubConnection.InvokeAsync(
                    RippleHubConstants.SubscribeMethod,
                    projectionType,
                    entityId,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        // Fetch initial data if not cached
        string cacheKey = CreateCacheKey<TProjection>(entityId);
        if (!cache.ContainsKey(cacheKey))
        {
            TProjection? data = await FetchProjectionAsync<TProjection>(entityId, cancellationToken)
                .ConfigureAwait(false);
            if (data != null)
            {
                cache.TryAdd(cacheKey, new CacheEntry<TProjection>(data));
            }
        }

        WasmProjectionSubscription<TProjection> subscription = new(
            this,
            entityId,
            onChanged);
        subscription.SubscriptionId = group.Add(subscription);
        return subscription;
    }

    /// <summary>
    ///     Notifies subscribers for a specific projection entity.
    /// </summary>
    /// <typeparam name="TProjection">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    internal void NotifySubscription<TProjection>(
        string entityId
    )
        where TProjection : class
    {
        string projectionType = typeof(TProjection).Name;
        string subscriptionKey = CreateSubscriptionKey(projectionType, entityId);
        if (subscriptions.TryGetValue(subscriptionKey, out SubscriptionGroup? group))
        {
            group.NotifyAll();
        }
    }

    /// <summary>
    ///     Removes a subscription from the group and unsubscribes from the hub if empty.
    /// </summary>
    /// <param name="projectionType">The projection type name.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="subscriptionId">The subscription identifier.</param>
    internal void RemoveSubscription(
        string projectionType,
        string entityId,
        int subscriptionId
    )
    {
        string subscriptionKey = CreateSubscriptionKey(projectionType, entityId);
        if (subscriptions.TryGetValue(subscriptionKey, out SubscriptionGroup? group))
        {
            group.Remove(subscriptionId);
            if (group.IsEmpty && subscriptions.TryRemove(subscriptionKey, out _))
            {
                // Unsubscribe from hub
                _ = hubConnection.InvokeAsync(
                    RippleHubConstants.UnsubscribeMethod,
                    projectionType,
                    entityId);
            }
        }
    }

    private static string CreateCacheKey<TProjection>(
        string entityId
    ) =>
        $"{typeof(TProjection).Name}:{entityId}";

    private static string CreateSubscriptionKey(
        string projectionType,
        string entityId
    ) =>
        $"{projectionType}:{entityId}";

    private async Task EnsureConnectedAsync(
        CancellationToken cancellationToken
    )
    {
        if (hubConnection.State == HubConnectionState.Connected)
        {
            return;
        }

        await connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (hubConnection.State == HubConnectionState.Connected)
            {
                return;
            }

            WasmProjectionCacheLoggerExtensions.StartingSignalRConnection(Logger);
            await hubConnection.StartAsync(cancellationToken).ConfigureAwait(false);
            WasmProjectionCacheLoggerExtensions.SignalRConnectionEstablished(Logger);
        }
        finally
        {
            connectionLock.Release();
        }
    }

    private async Task<TProjection?> FetchProjectionAsync<TProjection>(
        string entityId,
        CancellationToken cancellationToken
    )
        where TProjection : class
    {
        try
        {
            string projectionType = typeof(TProjection).Name;
            WasmProjectionCacheLoggerExtensions.FetchingProjection(Logger, projectionType, entityId);
            string url = $"/api/projections/{projectionType}/{entityId}";
            return await HttpClient.GetFromJsonAsync<TProjection>(url, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            WasmProjectionCacheLoggerExtensions.FetchProjectionFailed(Logger, ex.Message);
            return null;
        }
    }

    private Task OnProjectionUpdatedAsync(
        string projectionType,
        string entityId,
        long newVersion
    )
    {
        WasmProjectionCacheLoggerExtensions.ProjectionUpdateReceived(Logger, projectionType, entityId, newVersion);

        // Notify subscribers so they can refresh their data
        string subscriptionKey = CreateSubscriptionKey(projectionType, entityId);
        if (subscriptions.TryGetValue(subscriptionKey, out SubscriptionGroup? group))
        {
            group.NotifyAll();
        }

        return Task.CompletedTask;
    }

    private async Task OnReconnectedAsync(
        string? connectionId
    )
    {
        WasmProjectionCacheLoggerExtensions.SignalRReconnected(Logger, connectionId);

        // Re-subscribe to all active subscriptions
        foreach ((string _, SubscriptionGroup group) in subscriptions)
        {
            await hubConnection.InvokeAsync(
                    RippleHubConstants.SubscribeMethod,
                    group.ProjectionType,
                    group.EntityId)
                .ConfigureAwait(false);
        }
    }

    private sealed class CacheEntry<TProjection>
        where TProjection : class
    {
        public CacheEntry(
            TProjection data
        )
        {
            Data = data;
        }

        public TProjection Data { get; set; }
    }

    private sealed class SubscriptionGroup
    {
        private readonly ConcurrentDictionary<int, INotifiable> callbacks = new();

        private int nextId;

        public SubscriptionGroup(
            string projectionType,
            string entityId
        )
        {
            ProjectionType = projectionType;
            EntityId = entityId;
        }

        public string EntityId { get; }

        public bool IsEmpty => callbacks.IsEmpty;

        public string ProjectionType { get; }

        public int Add(
            INotifiable subscription
        )
        {
            int id = Interlocked.Increment(ref nextId);
            callbacks.TryAdd(id, subscription);
            return id;
        }

        public void NotifyAll()
        {
            foreach ((int _, INotifiable notifiable) in callbacks)
            {
                notifiable.Notify();
            }
        }

        public void Remove(
            int subscriptionId
        ) =>
            callbacks.TryRemove(subscriptionId, out _);
    }

    /// <summary>
    ///     Interface for notifiable subscriptions.
    /// </summary>
    private interface INotifiable
    {
        /// <summary>
        ///     Gets or sets the subscription identifier for removal.
        /// </summary>
        int SubscriptionId { get; set; }

        /// <summary>
        ///     Notifies the subscription that data has changed.
        /// </summary>
        void Notify();
    }

    /// <summary>
    ///     A binder implementation for WASM clients.
    /// </summary>
    /// <typeparam name="TProjection">The projection type.</typeparam>
    private sealed class WasmProjectionBinder<TProjection> : IProjectionBinder<TProjection>, INotifiable
        where TProjection : class
    {
        private readonly WasmProjectionCache cache;

        private Action? onChanged;

        private WasmProjectionSubscription<TProjection>? subscription;

        public WasmProjectionBinder(
            WasmProjectionCache cache
        )
        {
            this.cache = cache;
        }

        /// <inheritdoc />
        public TProjection? Current => subscription?.Current;

        /// <inheritdoc />
        public string? EntityId { get; private set; }

        /// <inheritdoc />
        public bool IsLoaded => subscription?.IsLoaded ?? false;

        /// <inheritdoc />
        public bool IsLoading => subscription?.IsLoading ?? false;

        /// <inheritdoc />
        public Exception? LastError => subscription?.LastError;

        /// <inheritdoc />
        int INotifiable.SubscriptionId { get; set; }

        /// <inheritdoc />
        public async Task BindAsync(
            string entityId,
            Action onChanged,
            CancellationToken cancellationToken = default
        )
        {
            if (EntityId == entityId)
            {
                return;
            }

            await UnbindAsync().ConfigureAwait(false);
            EntityId = entityId;
            this.onChanged = onChanged;
            IProjectionSubscription<TProjection> newSubscription = await cache
                .SubscribeAsync<TProjection>(entityId, onChanged, cancellationToken)
                .ConfigureAwait(false);
            subscription = (WasmProjectionSubscription<TProjection>)newSubscription;
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync() => await UnbindAsync().ConfigureAwait(false);

        /// <inheritdoc />
        public void Notify() => onChanged?.Invoke();

        /// <inheritdoc />
        public async Task UnbindAsync()
        {
            if (subscription != null)
            {
                await subscription.DisposeAsync().ConfigureAwait(false);
                subscription = null;
            }

            EntityId = null;
        }
    }

    /// <summary>
    ///     A subscription implementation for WASM clients.
    /// </summary>
    /// <typeparam name="TProjection">The projection type.</typeparam>
    private sealed class WasmProjectionSubscription<TProjection> : IProjectionSubscription<TProjection>, INotifiable
        where TProjection : class
    {
        private readonly WasmProjectionCache cache;

        private readonly Action onChanged;

        private bool isDisposed;

        public WasmProjectionSubscription(
            WasmProjectionCache cache,
            string entityId,
            Action onChanged
        )
        {
            this.cache = cache;
            EntityId = entityId;
            this.onChanged = onChanged;
        }

        /// <inheritdoc />
        public TProjection? Current => cache.GetCached<TProjection>(EntityId);

        /// <inheritdoc />
        public string EntityId { get; }

        /// <inheritdoc />
        public bool IsLoaded => Current != null;

        /// <inheritdoc />
        public bool IsLoading { get; internal set; }

        /// <inheritdoc />
        public Exception? LastError { get; internal set; }

        /// <inheritdoc />
        public int SubscriptionId { get; set; }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            if (isDisposed)
            {
                return ValueTask.CompletedTask;
            }

            isDisposed = true;
            string projectionType = typeof(TProjection).Name;
            cache.RemoveSubscription(projectionType, EntityId, SubscriptionId);
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc />
        public void Notify()
        {
            if (!isDisposed)
            {
                onChanged();
            }
        }
    }
}
