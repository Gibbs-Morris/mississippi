using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.UxProjections.Abstractions;
using Mississippi.Ripples.Abstractions;


namespace Mississippi.Ripples.Server;

/// <summary>
///     Server-side ripple pool for managing multiple projection subscriptions.
/// </summary>
/// <typeparam name="TProjection">The projection state type.</typeparam>
/// <remarks>
///     <para>
///         <see cref="ServerRipplePool{TProjection}" /> is designed for list/detail scenarios
///         where a parent projection contains a list of IDs, and each ID needs its own
///         subscription. It efficiently manages multiple subscriptions with tiered caching.
///     </para>
/// </remarks>
internal sealed class ServerRipplePool<TProjection> : IRipplePool<TProjection>
    where TProjection : class
{
    private readonly ConcurrentDictionary<string, PoolEntry> entries = new(StringComparer.Ordinal);

    private readonly SemaphoreSlim subscriptionLock = new(1, 1);

    private int cacheHits;

    private bool isDisposed;

    private int totalFetches;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ServerRipplePool{TProjection}" /> class.
    /// </summary>
    /// <param name="projectionGrainFactory">Factory for resolving UX projection grains.</param>
    /// <param name="signalRNotifier">SignalR notifier for real-time updates.</param>
    /// <param name="loggerFactory">Factory for creating loggers.</param>
    public ServerRipplePool(
        IUxProjectionGrainFactory projectionGrainFactory,
        IProjectionUpdateNotifier signalRNotifier,
        ILoggerFactory loggerFactory
    )
    {
        ArgumentNullException.ThrowIfNull(projectionGrainFactory);
        ArgumentNullException.ThrowIfNull(signalRNotifier);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ProjectionGrainFactory = projectionGrainFactory;
        SignalRNotifier = signalRNotifier;
        LoggerFactory = loggerFactory;
        Logger = loggerFactory.CreateLogger<ServerRipplePool<TProjection>>();
    }

    /// <inheritdoc />
    public RipplePoolStats Stats
    {
        get
        {
            int hotCount = entries.Values.Count(e => e.IsHot);
            int warmCount = entries.Values.Count(e => !e.IsHot);
            return new(hotCount, warmCount, totalFetches, cacheHits);
        }
    }

    private ILogger<ServerRipplePool<TProjection>> Logger { get; }

    private ILoggerFactory LoggerFactory { get; }

    private IUxProjectionGrainFactory ProjectionGrainFactory { get; }

    private IProjectionUpdateNotifier SignalRNotifier { get; }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (isDisposed)
        {
            return;
        }

        await subscriptionLock.WaitAsync().ConfigureAwait(false);
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
            subscriptionLock.Release();
            subscriptionLock.Dispose();
        }
    }

    /// <inheritdoc />
    public IRipple<TProjection> GetOrCreate(
        string entityId
    )
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
        entry = new(ripple)
        {
            IsHot = true,
        };
        entries[entityId] = entry;
        Logger.PoolSubscribed(typeof(TProjection).Name, 1, 1);
        return ripple;
    }

    /// <inheritdoc />
    public void MarkHidden(
        string entityId
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(entityId);
        if (entries.TryGetValue(entityId, out PoolEntry? entry))
        {
            entry.IsHot = false;
        }
    }

    /// <inheritdoc />
    public void MarkVisible(
        string entityId
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(entityId);
        if (entries.TryGetValue(entityId, out PoolEntry? entry))
        {
            entry.IsHot = true;
        }
    }

    /// <inheritdoc />
    public async Task PrefetchAsync(
        IEnumerable<string> entityIds,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(entityIds);
        IReadOnlyList<string> idList = entityIds as IReadOnlyList<string> ?? entityIds.ToList();
        List<string> newIds = idList.Where(id => !entries.ContainsKey(id)).ToList();
        if (newIds.Count == 0)
        {
            return;
        }

        await subscriptionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);

            // Create entries for new IDs
            foreach (string id in newIds.Where(id => !entries.ContainsKey(id)))
            {
                ServerRipple<TProjection> ripple = CreateRipple();
                entries[id] = new(ripple)
                {
                    IsHot = false,
                };
                Interlocked.Increment(ref totalFetches);
            }

            // Prefetch all new projections in parallel
            List<Task> fetchTasks = newIds.Where(id => entries.ContainsKey(id))
                .Select(id => entries[id].Ripple.SubscribeAsync(id, cancellationToken))
                .ToList();
            await Task.WhenAll(fetchTasks).ConfigureAwait(false);
            Logger.PoolSubscribed(typeof(TProjection).Name, idList.Count, newIds.Count);
        }
        finally
        {
            subscriptionLock.Release();
        }
    }

    private ServerRipple<TProjection> CreateRipple()
    {
        ILogger<ServerRipple<TProjection>> rippleLogger = LoggerFactory.CreateLogger<ServerRipple<TProjection>>();
        return new(ProjectionGrainFactory, SignalRNotifier, rippleLogger);
    }

    private sealed class PoolEntry
    {
        public PoolEntry(
            ServerRipple<TProjection> ripple
        ) =>
            Ripple = ripple;

        public bool IsHot { get; set; }

        public ServerRipple<TProjection> Ripple { get; }
    }
}