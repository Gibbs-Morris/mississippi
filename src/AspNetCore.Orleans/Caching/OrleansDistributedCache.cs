namespace Mississippi.AspNetCore.Orleans.Caching;

using System;
using System.Threading;
using System.Threading.Tasks;
using global::Orleans;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mississippi.AspNetCore.Orleans.Caching.Grains;
using Mississippi.AspNetCore.Orleans.Caching.Options;

/// <summary>
/// Orleans-backed implementation of <see cref="IDistributedCache"/>.
/// Uses per-key grains for scalable distributed caching.
/// </summary>
public sealed class OrleansDistributedCache : IDistributedCache
{
    private ILogger<OrleansDistributedCache> Logger { get; }
    private IClusterClient ClusterClient { get; }
    private IOptions<DistributedCacheOptions> Options { get; }
    private TimeProvider TimeProvider { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrleansDistributedCache"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="clusterClient">The Orleans cluster client.</param>
    /// <param name="options">The distributed cache options.</param>
    /// <param name="timeProvider">The time provider for expiration handling.</param>
    public OrleansDistributedCache(
        ILogger<OrleansDistributedCache> logger,
        IClusterClient clusterClient,
        IOptions<DistributedCacheOptions> options,
        TimeProvider timeProvider)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ClusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc/>
    public byte[]? Get(string key)
    {
        return GetAsync(key).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        string grainKey = GetGrainKey(key);
        ICacheEntryGrain grain = ClusterClient.GetGrain<ICacheEntryGrain>(grainKey);
        CacheEntryData? data = await grain.GetAsync();

        if (data is null)
        {
            OrleansDistributedCacheLoggerExtensions.CacheMiss(Logger, key);
            return null;
        }

        OrleansDistributedCacheLoggerExtensions.CacheHit(Logger, key, data.Value.Length);
        return data.Value;
    }

    /// <inheritdoc/>
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        SetAsync(key, value, options).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        DateTimeOffset now = TimeProvider.GetUtcNow();
        DateTimeOffset? absoluteExpiration = null;
        TimeSpan? slidingExpiration = null;

        if (options.AbsoluteExpiration.HasValue)
        {
            absoluteExpiration = options.AbsoluteExpiration.Value;
        }
        else if (options.AbsoluteExpirationRelativeToNow.HasValue)
        {
            absoluteExpiration = now + options.AbsoluteExpirationRelativeToNow.Value;
        }
        else if (Options.Value.DefaultAbsoluteExpirationRelativeToNow.HasValue)
        {
            absoluteExpiration = now + Options.Value.DefaultAbsoluteExpirationRelativeToNow.Value;
        }

        if (options.SlidingExpiration.HasValue)
        {
            slidingExpiration = options.SlidingExpiration.Value;
        }
        else if (Options.Value.DefaultSlidingExpiration.HasValue)
        {
            slidingExpiration = Options.Value.DefaultSlidingExpiration.Value;
        }

        CacheEntryData data = new()
        {
            Value = value,
            AbsoluteExpiration = absoluteExpiration,
            SlidingExpiration = slidingExpiration,
            LastAccessTime = now
        };

        string grainKey = GetGrainKey(key);
        ICacheEntryGrain grain = ClusterClient.GetGrain<ICacheEntryGrain>(grainKey);
        await grain.SetAsync(data);

        OrleansDistributedCacheLoggerExtensions.CacheSet(Logger, key, value.Length);
    }

    /// <inheritdoc/>
    public void Refresh(string key)
    {
        RefreshAsync(key).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        string grainKey = GetGrainKey(key);
        ICacheEntryGrain grain = ClusterClient.GetGrain<ICacheEntryGrain>(grainKey);
        await grain.RefreshAsync();

        OrleansDistributedCacheLoggerExtensions.CacheRefreshed(Logger, key);
    }

    /// <inheritdoc/>
    public void Remove(string key)
    {
        RemoveAsync(key).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        string grainKey = GetGrainKey(key);
        ICacheEntryGrain grain = ClusterClient.GetGrain<ICacheEntryGrain>(grainKey);
        await grain.RemoveAsync();

        OrleansDistributedCacheLoggerExtensions.CacheRemoved(Logger, key);
    }

    private string GetGrainKey(string key)
    {
        return $"{Options.Value.KeyPrefix}{key}";
    }
}

/// <summary>
/// High-performance logger extensions for distributed cache operations.
/// </summary>
internal static class OrleansDistributedCacheLoggerExtensions
{
    private static readonly Action<ILogger, string, Exception?> CacheMissMessage =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(1, nameof(CacheMiss)),
            "Cache miss: Key={Key}");

    private static readonly Action<ILogger, string, int, Exception?> CacheHitMessage =
        LoggerMessage.Define<string, int>(
            LogLevel.Debug,
            new EventId(2, nameof(CacheHit)),
            "Cache hit: Key={Key}, Size={Size}");

    private static readonly Action<ILogger, string, int, Exception?> CacheSetMessage =
        LoggerMessage.Define<string, int>(
            LogLevel.Debug,
            new EventId(3, nameof(CacheSet)),
            "Cache set: Key={Key}, Size={Size}");

    private static readonly Action<ILogger, string, Exception?> CacheRefreshedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(4, nameof(CacheRefreshed)),
            "Cache refreshed: Key={Key}");

    private static readonly Action<ILogger, string, Exception?> CacheRemovedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(5, nameof(CacheRemoved)),
            "Cache removed: Key={Key}");

    public static void CacheMiss(this ILogger<OrleansDistributedCache> logger, string key) =>
        CacheMissMessage(logger, key, null);

    public static void CacheHit(this ILogger<OrleansDistributedCache> logger, string key, int size) =>
        CacheHitMessage(logger, key, size, null);

    public static void CacheSet(this ILogger<OrleansDistributedCache> logger, string key, int size) =>
        CacheSetMessage(logger, key, size, null);

    public static void CacheRefreshed(this ILogger<OrleansDistributedCache> logger, string key) =>
        CacheRefreshedMessage(logger, key, null);

    public static void CacheRemoved(this ILogger<OrleansDistributedCache> logger, string key) =>
        CacheRemovedMessage(logger, key, null);
}
