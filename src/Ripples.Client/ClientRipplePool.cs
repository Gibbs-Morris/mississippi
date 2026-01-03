namespace Mississippi.Ripples.Client;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.Ripples.Abstractions;

/// <summary>
/// Client-side implementation of <see cref="IRipplePool{TProjection}"/> for managing multiple projections.
/// </summary>
/// <typeparam name="TProjection">The type of projection.</typeparam>
internal sealed class ClientRipplePool<TProjection> : IRipplePool<TProjection>
    where TProjection : class
{
    private readonly ConcurrentDictionary<string, PoolEntry> entries = new();
    private int totalFetches;
    private int cacheHits;

    private HttpClient HttpClient { get; }

    private ISignalRRippleConnection SignalRConnection { get; }

    private IProjectionRouteProvider RouteProvider { get; }

    private ILoggerFactory LoggerFactory { get; }

    private ILogger<ClientRipplePool<TProjection>> Logger { get; }

    private bool isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientRipplePool{TProjection}"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="signalRConnection">The SignalR connection.</param>
    /// <param name="routeProvider">The route provider.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public ClientRipplePool(
        HttpClient httpClient,
        ISignalRRippleConnection signalRConnection,
        IProjectionRouteProvider routeProvider,
        ILoggerFactory loggerFactory)
    {
        HttpClient = httpClient;
        SignalRConnection = signalRConnection;
        RouteProvider = routeProvider;
        LoggerFactory = loggerFactory;
        Logger = loggerFactory.CreateLogger<ClientRipplePool<TProjection>>();
    }

    /// <inheritdoc/>
    public RipplePoolStats Stats
    {
        get
        {
            int hotCount = 0;
            int warmCount = 0;

            foreach ((_, PoolEntry entry) in entries)
            {
                if (entry.IsHot)
                {
                    hotCount++;
                }
                else
                {
                    warmCount++;
                }
            }

            return new RipplePoolStats(hotCount, warmCount, totalFetches, cacheHits);
        }
    }

    /// <inheritdoc/>
    public IRipple<TProjection> GetOrCreate(string entityId)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        ArgumentException.ThrowIfNullOrEmpty(entityId);

        bool wasCreated = false;
        PoolEntry entry = entries.GetOrAdd(entityId, _ =>
        {
            wasCreated = true;
            return new PoolEntry(CreateRipple());
        });

        if (!wasCreated)
        {
            Interlocked.Increment(ref cacheHits);
        }

        Interlocked.Increment(ref totalFetches);
        entry.MarkHot();

        ClientRipplePoolLoggerExtensions.GetOrCreateRipple(Logger, typeof(TProjection).Name, entityId);
        return entry.Ripple;
    }

    /// <inheritdoc/>
    public void MarkHidden(string entityId)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        ArgumentException.ThrowIfNullOrEmpty(entityId);

        if (entries.TryGetValue(entityId, out PoolEntry? entry))
        {
            entry.MarkWarm();
            ClientRipplePoolLoggerExtensions.MarkedHidden(Logger, typeof(TProjection).Name, entityId);
        }
    }

    /// <inheritdoc/>
    public void MarkVisible(string entityId)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        ArgumentException.ThrowIfNullOrEmpty(entityId);

        if (entries.TryGetValue(entityId, out PoolEntry? entry))
        {
            entry.MarkHot();
            ClientRipplePoolLoggerExtensions.MarkedVisible(Logger, typeof(TProjection).Name, entityId);
        }
    }

    /// <inheritdoc/>
    public async Task PrefetchAsync(
        IEnumerable<string> entityIds,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        ArgumentNullException.ThrowIfNull(entityIds);

        IReadOnlyList<string> entityIdList = entityIds as IReadOnlyList<string> ?? entityIds.ToList();

        if (entityIdList.Count == 0)
        {
            return;
        }

        ClientRipplePoolLoggerExtensions.PrefetchingProjections(
            Logger,
            typeof(TProjection).Name,
            entityIdList.Count);

        // Try batch endpoint first
        string route = RouteProvider.GetRoute<TProjection>();
        string batchUrl = $"{route}/batch";

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Post, batchUrl)
            {
                Content = JsonContent.Create(new { EntityIds = entityIdList }),
            };

            using HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                Dictionary<string, TProjection>? batch = await response.Content
                    .ReadFromJsonAsync<Dictionary<string, TProjection>>(cancellationToken)
                    .ConfigureAwait(false);

                if (batch != null)
                {
                    foreach ((string entityId, TProjection projection) in batch)
                    {
                        PoolEntry entry = entries.GetOrAdd(entityId, _ => new PoolEntry(CreateRipple()));
                        entry.Ripple.SetPrefetchedData(projection);
                    }

                    ClientRipplePoolLoggerExtensions.BatchPrefetchSucceeded(
                        Logger,
                        typeof(TProjection).Name,
                        batch.Count);
                    return;
                }
            }
        }
        catch (HttpRequestException)
        {
            // Batch endpoint may not be available, fall back to individual fetches
            ClientRipplePoolLoggerExtensions.BatchEndpointUnavailable(Logger, typeof(TProjection).Name);
        }

        // Fall back to individual subscriptions
        IEnumerable<Task> subscriptionTasks = entityIdList.Select(async entityId =>
        {
            PoolEntry entry = entries.GetOrAdd(entityId, _ => new PoolEntry(CreateRipple()));
            await entry.Ripple.SubscribeAsync(entityId, cancellationToken).ConfigureAwait(false);
        });

        await Task.WhenAll(subscriptionTasks).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;

        IEnumerable<Task> disposeTasks = entries.Values.Select(async entry =>
        {
            await entry.Ripple.DisposeAsync().ConfigureAwait(false);
        });

        await Task.WhenAll(disposeTasks).ConfigureAwait(false);
        entries.Clear();
    }

    private ClientRipple<TProjection> CreateRipple()
    {
        ILogger<ClientRipple<TProjection>> rippleLogger = LoggerFactory.CreateLogger<ClientRipple<TProjection>>();
        return new ClientRipple<TProjection>(HttpClient, SignalRConnection, RouteProvider, rippleLogger);
    }

    private sealed class PoolEntry(ClientRipple<TProjection> ripple)
    {
        public ClientRipple<TProjection> Ripple { get; } = ripple;

        public bool IsHot { get; private set; } = true;

        public void MarkHot() => IsHot = true;

        public void MarkWarm() => IsHot = false;
    }
}
