namespace Mississippi.Ripples.Server;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mississippi.EventSourcing.UxProjections.Abstractions;
using Mississippi.Ripples.Abstractions;

/// <summary>
/// Server-side ripple pool for managing multiple projection subscriptions.
/// </summary>
/// <typeparam name="TProjection">The projection state type.</typeparam>
/// <remarks>
/// <para>
/// <see cref="ServerRipplePool{TProjection}"/> is designed for list/detail scenarios
/// where a parent projection contains a list of IDs, and each ID needs its own
/// subscription. It efficiently manages multiple subscriptions with tiered caching.
/// </para>
/// </remarks>
public sealed class ServerRipplePool<TProjection> : IRipplePool<TProjection>
    where TProjection : class
{
    private readonly ConcurrentDictionary<string, PoolEntry> entries = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private bool isDisposed;
    private int totalFetches;
    private int cacheHits;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerRipplePool{TProjection}"/> class.
    /// </summary>
    /// <param name="projectionGrainFactory">Factory for resolving UX projection grains.</param>
    /// <param name="signalRNotifier">SignalR notifier for real-time updates.</param>
    /// <param name="loggerFactory">Factory for creating loggers.</param>
    public ServerRipplePool(
        IUxProjectionGrainFactory projectionGrainFactory,
        IProjectionUpdateNotifier signalRNotifier,
        ILoggerFactory loggerFactory)
    {
        ProjectionGrainFactory = projectionGrainFactory ??
            throw new ArgumentNullException(nameof(projectionGrainFactory));
        SignalRNotifier = signalRNotifier ??
            throw new ArgumentNullException(nameof(signalRNotifier));
        LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        Logger = loggerFactory.CreateLogger<ServerRipplePool<TProjection>>();
    }

    private IUxProjectionGrainFactory ProjectionGrainFactory { get; }

    private IProjectionUpdateNotifier SignalRNotifier { get; }

    private ILoggerFactory LoggerFactory { get; }

    private ILogger<ServerRipplePool<TProjection>> Logger { get; }

    /// <inheritdoc/>
    public RipplePoolStats Stats
    {
        get
        {
            int hotCount = entries.Values.Count(e => e.IsHot);
            int warmCount = entries.Values.Count(e => !e.IsHot);

            return new RipplePoolStats(hotCount, warmCount, totalFetches, cacheHits);
        }
    }

    /// <inheritdoc/>
    public IRipple<TProjection> GetOrCreate(string entityId)
    {
        ArgumentException.ThrowIfNullOrEmpty(entityId);

        if (entries.TryGetValue(entityId, out PoolEntry? entry))
        {
            Interlocked.Increment(ref cacheHits);
            entry.IsHot = true;

            return entry.Ripple;
        }

        Interlocked.Increment(ref totalFetches);

        ServerRipple<TProjection> ripple = CreateRipple();

        entry = new PoolEntry(ripple) { IsHot = true };
        entries[entityId] = entry;

        Logger.PoolSubscribed(typeof(TProjection).Name, 1, 1);

        return ripple;
    }

    /// <inheritdoc/>
    public void MarkHidden(string entityId)
    {
        ArgumentException.ThrowIfNullOrEmpty(entityId);

        if (entries.TryGetValue(entityId, out PoolEntry? entry))
        {
            entry.IsHot = false;
        }
    }

    /// <inheritdoc/>
    public void MarkVisible(string entityId)
    {
        ArgumentException.ThrowIfNullOrEmpty(entityId);

        if (entries.TryGetValue(entityId, out PoolEntry? entry))
        {
            entry.IsHot = true;
        }
    }

    /// <inheritdoc/>
    public async Task PrefetchAsync(
        IEnumerable<string> entityIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entityIds);

        IReadOnlyList<string> idList = entityIds as IReadOnlyList<string> ?? entityIds.ToList();

        List<string> newIds = idList.Where(id => !entries.ContainsKey(id)).ToList();

        if (newIds.Count == 0)
        {
            return;
        }

        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);

            // Create entries for new IDs
            foreach (string id in newIds.Where(id => !entries.ContainsKey(id)))
            {
                ServerRipple<TProjection> ripple = CreateRipple();
                entries[id] = new PoolEntry(ripple) { IsHot = false };
                Interlocked.Increment(ref totalFetches);
            }

            // Prefetch all new projections in parallel
            List<Task> fetchTasks = newIds
                .Where(id => entries.ContainsKey(id))
                .Select(id => entries[id].Ripple.SubscribeAsync(id, cancellationToken))
                .ToList();

            await Task.WhenAll(fetchTasks).ConfigureAwait(false);

            Logger.PoolSubscribed(typeof(TProjection).Name, idList.Count, newIds.Count);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (isDisposed)
        {
            return;
        }

        await semaphore.WaitAsync().ConfigureAwait(false);

        try
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;

            foreach (PoolEntry entry in entries.Values)
            {
                await entry.Ripple.DisposeAsync().ConfigureAwait(false);
            }

            entries.Clear();
        }
        finally
        {
            semaphore.Release();
            semaphore.Dispose();
        }
    }

    private ServerRipple<TProjection> CreateRipple()
    {
        ILogger<ServerRipple<TProjection>> rippleLogger =
            LoggerFactory.CreateLogger<ServerRipple<TProjection>>();

        return new ServerRipple<TProjection>(
            ProjectionGrainFactory,
            SignalRNotifier,
            rippleLogger);
    }

    private sealed class PoolEntry
    {
        public PoolEntry(ServerRipple<TProjection> ripple)
        {
            Ripple = ripple;
        }

        public ServerRipple<TProjection> Ripple { get; }

        public bool IsHot { get; set; }
    }
}
