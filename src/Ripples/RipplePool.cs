using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Ripples.Abstractions;


namespace Mississippi.Ripples;

/// <summary>
///     Manages a pool of ripples for list/detail patterns.
///     Handles subscription lifecycle, caching, and batching using Hot/Warm tier semantics.
/// </summary>
/// <typeparam name="T">The projection type.</typeparam>
public sealed class RipplePool<T> : IRipplePool<T>
    where T : class
{
    private readonly ConcurrentDictionary<string, bool> hotEntities = new(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<string, IRipple<T>> ripples = new(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<string, bool> warmEntities = new(StringComparer.Ordinal);

    private int cacheHits;

    private bool disposed;

    private int totalFetches;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RipplePool{T}" /> class.
    /// </summary>
    /// <param name="factory">The factory used to create ripple instances.</param>
    /// <param name="options">The pool configuration options.</param>
    /// <exception cref="ArgumentNullException">Thrown when factory or options is null.</exception>
    public RipplePool(
        IRippleFactory<T> factory,
        RipplePoolOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(options);
        Factory = factory;
        Options = options;
    }

    /// <inheritdoc />
    public RipplePoolStats Stats =>
        new(hotEntities.Count, warmEntities.Count, Volatile.Read(ref totalFetches), Volatile.Read(ref cacheHits));

    private IRippleFactory<T> Factory { get; }

    private RipplePoolOptions Options { get; }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        IEnumerable<Task> disposeTasks = ripples.Values.Select(async ripple =>
        {
            await ripple.DisposeAsync().ConfigureAwait(false);
        });
        await Task.WhenAll(disposeTasks).ConfigureAwait(false);
        ripples.Clear();
        hotEntities.Clear();
        warmEntities.Clear();
    }

    /// <inheritdoc />
    public IRipple<T> GetOrCreate(
        string entityId
    )
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(entityId);
        bool isFromCache = true;
        IRipple<T> ripple = ripples.GetOrAdd(
            entityId,
            _ =>
            {
                isFromCache = false;
                Interlocked.Increment(ref totalFetches);
                return Factory.Create();
            });
        if (isFromCache && warmEntities.ContainsKey(entityId))
        {
            Interlocked.Increment(ref cacheHits);
        }

        return ripple;
    }

    /// <inheritdoc />
    public void MarkHidden(
        string entityId
    )
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(entityId);
        if (!ripples.ContainsKey(entityId))
        {
            return;
        }

        // Remove from hot if present
        if (hotEntities.TryRemove(entityId, out bool _))
        {
            // Move to warm tier
            warmEntities.TryAdd(entityId, true);
        }
    }

    /// <inheritdoc />
    public void MarkVisible(
        string entityId
    )
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(entityId);
        if (!ripples.ContainsKey(entityId))
        {
            return;
        }

        // Remove from warm if present
        warmEntities.TryRemove(entityId, out bool _);

        // Add to hot (idempotent)
        hotEntities.TryAdd(entityId, true);
    }

    /// <inheritdoc />
    public async Task PrefetchAsync(
        IEnumerable<string> entityIds,
        CancellationToken cancellationToken = default
    )
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(entityIds);
        List<string> idsToFetch = entityIds.Where(id => !ripples.ContainsKey(id)).ToList();
        if (idsToFetch.Count == 0)
        {
            return;
        }

        // Create ripples for each entity that isn't already cached
        IEnumerable<Task> tasks = idsToFetch.Select(async entityId =>
        {
            IRipple<T> ripple = GetOrCreate(entityId);
            await ripple.SubscribeAsync(entityId, cancellationToken).ConfigureAwait(false);
        });
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}