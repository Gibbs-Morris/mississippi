using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Ripples.Abstractions;


namespace Mississippi.Ripples.Blazor;

/// <summary>
///     Circuit-scoped projection cache with LRU eviction.
/// </summary>
/// <remarks>
///     <para>
///         This implementation maintains projection data across page navigations.
///         When a component subscribes to a projection:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>If cached: returns immediately with existing data</description>
///         </item>
///         <item>
///             <description>If not cached: creates a new ripple and fetches data</description>
///         </item>
///     </list>
///     <para>
///         When a subscription is disposed:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>The callback is removed</description>
///         </item>
///         <item>
///             <description>Data stays cached for quick re-subscription</description>
///         </item>
///         <item>
///             <description>LRU eviction removes oldest unused entries when at capacity</description>
///         </item>
///     </list>
/// </remarks>
public sealed class ProjectionCache : IProjectionCache
{
    private readonly ConcurrentDictionary<(Type ProjectionType, string EntityId), CacheEntry> cache = new();

    private readonly object evictionLock = new();

    private readonly LinkedList<(Type ProjectionType, string EntityId)> lruList = new();

    private readonly int maxCacheSize;

    private readonly IServiceProvider serviceProvider;

    private bool disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionCache" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving ripple factories.</param>
    /// <param name="maxCacheSize">Maximum number of cached projections before LRU eviction.</param>
    public ProjectionCache(
        IServiceProvider serviceProvider,
        int maxCacheSize = 50
    )
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        this.serviceProvider = serviceProvider;
        this.maxCacheSize = maxCacheSize;
    }

    /// <inheritdoc />
    public IProjectionBinder<TProjection> CreateBinder<TProjection>()
        where TProjection : class =>
        new ProjectionBinder<TProjection>(this);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        foreach (KeyValuePair<(Type ProjectionType, string EntityId), CacheEntry> kvp in cache)
        {
            await kvp.Value.DisposeAsync().ConfigureAwait(false);
        }

        cache.Clear();
        lock (evictionLock)
        {
            lruList.Clear();
        }
    }

    /// <inheritdoc />
    public Task EvictAsync<TProjection>(
        string entityId
    )
        where TProjection : class
    {
        (Type ProjectionType, string EntityId) key = (typeof(TProjection), entityId);
        if (cache.TryRemove(key, out CacheEntry? entry))
        {
            lock (evictionLock)
            {
                if (entry.LruNode is not null)
                {
                    lruList.Remove(entry.LruNode);
                }
            }

            return entry.DisposeAsync().AsTask();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public TProjection? GetCached<TProjection>(
        string entityId
    )
        where TProjection : class
    {
        (Type ProjectionType, string EntityId) key = (typeof(TProjection), entityId);
        if (cache.TryGetValue(key, out CacheEntry? entry))
        {
            TouchLru(key, entry);
            return ((IRipple<TProjection>)entry.Ripple).Current;
        }

        return null;
    }

    /// <inheritdoc />
    public bool IsCached<TProjection>(
        string entityId
    )
        where TProjection : class =>
        cache.ContainsKey((typeof(TProjection), entityId));

    /// <inheritdoc />
    public async Task RefreshAsync<TProjection>(
        string entityId,
        CancellationToken cancellationToken = default
    )
        where TProjection : class
    {
        (Type ProjectionType, string EntityId) key = (typeof(TProjection), entityId);
        if (cache.TryGetValue(key, out CacheEntry? entry))
        {
            IRipple<TProjection> ripple = (IRipple<TProjection>)entry.Ripple;
            await ripple.RefreshAsync(cancellationToken).ConfigureAwait(false);
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
        ArgumentNullException.ThrowIfNull(entityId);
        ArgumentException.ThrowIfNullOrEmpty(entityId);
        ArgumentNullException.ThrowIfNull(onChanged);
        ObjectDisposedException.ThrowIf(disposed, this);
        (Type ProjectionType, string EntityId) key = (typeof(TProjection), entityId);

        // Try to get or create cache entry
        CacheEntry entry = cache.GetOrAdd(key, _ => CreateCacheEntry<TProjection>());

        // Update LRU
        TouchLru(key, entry);
        IRipple<TProjection> ripple = (IRipple<TProjection>)entry.Ripple;

        // Subscribe if not already subscribed (first subscriber)
        if (!ripple.IsLoaded && !ripple.IsLoading)
        {
            await ripple.SubscribeAsync(entityId, cancellationToken).ConfigureAwait(false);
        }

        // Increment reference count
        entry.IncrementSubscribers();

        // Create subscription handle
        ProjectionSubscription<TProjection> subscription = new(
            entityId,
            ripple,
            onChanged,
            _ => OnSubscriptionDisposed(key));
        return subscription;
    }

    private CacheEntry CreateCacheEntry<TProjection>()
        where TProjection : class
    {
        // Check if we need to evict
        EvictIfNeeded();

        // Create new ripple via factory
        IRippleFactory<TProjection> factory = serviceProvider.GetRequiredService<IRippleFactory<TProjection>>();
        IRipple<TProjection> ripple = factory.Create();
        return new(ripple);
    }

    private void EvictIfNeeded()
    {
        while (cache.Count >= maxCacheSize)
        {
            (Type ProjectionType, string EntityId)? keyToEvict = null;
            lock (evictionLock)
            {
                // Find oldest entry with no active subscribers
                LinkedListNode<(Type ProjectionType, string EntityId)>? node = lruList.First;
                while (node is not null)
                {
                    if (cache.TryGetValue(node.Value, out CacheEntry? entry) && (entry.SubscriberCount == 0))
                    {
                        keyToEvict = node.Value;
                        lruList.Remove(node);
                        break;
                    }

                    node = node.Next;
                }
            }

            if (keyToEvict is null)
            {
                // All entries have active subscribers, can't evict
                break;
            }

            // Remove from cache and dispose asynchronously
#pragma warning disable CA2000, IDISP007 // Ownership transfers to DisposeEntryAsync
            if (cache.TryRemove(keyToEvict.Value, out CacheEntry? removed))
            {
                // Dispose on background thread to avoid blocking
                _ = DisposeEntryAsync(removed);
            }
#pragma warning restore CA2000, IDISP007
        }
    }

    private void OnSubscriptionDisposed(
        (Type ProjectionType, string EntityId) key
    )
    {
        if (cache.TryGetValue(key, out CacheEntry? entry))
        {
            entry.DecrementSubscribers();
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Background disposal should not throw")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP007:Don't dispose injected",
        Justification = "Entry ownership is transferred from cache on removal")]
    private static async Task DisposeEntryAsync(
        CacheEntry entry
    )
    {
        try
        {
            await entry.DisposeAsync().ConfigureAwait(false);
        }
        catch
        {
            // Swallow exceptions during background disposal
        }
    }

    private void TouchLru(
        (Type ProjectionType, string EntityId) key,
        CacheEntry entry
    )
    {
        lock (evictionLock)
        {
            // Remove from current position
            if (entry.LruNode is not null)
            {
                lruList.Remove(entry.LruNode);
            }

            // Add to end (most recently used)
            entry.LruNode = lruList.AddLast(key);
        }
    }

    /// <summary>
    ///     Represents a cached projection entry.
    /// </summary>
    private sealed class CacheEntry : IAsyncDisposable
    {
        private int subscriberCount;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CacheEntry" /> class.
        /// </summary>
        /// <param name="ripple">The ripple instance.</param>
        public CacheEntry(
            object ripple
        ) =>
            Ripple = ripple;

        /// <summary>
        ///     Gets or sets the LRU list node for this entry.
        /// </summary>
        public LinkedListNode<(Type ProjectionType, string EntityId)>? LruNode { get; set; }

        /// <summary>
        ///     Gets the ripple instance.
        /// </summary>
        public object Ripple { get; }

        /// <summary>
        ///     Gets the number of active subscriptions to this entry.
        /// </summary>
        public int SubscriberCount => subscriberCount;

        /// <summary>
        ///     Decrements the subscriber count.
        /// </summary>
        public void DecrementSubscribers() => Interlocked.Decrement(ref subscriberCount);

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (Ripple is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Increments the subscriber count.
        /// </summary>
        public void IncrementSubscribers() => Interlocked.Increment(ref subscriberCount);
    }
}