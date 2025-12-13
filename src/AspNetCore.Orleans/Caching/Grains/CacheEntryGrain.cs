namespace Mississippi.AspNetCore.Orleans.Caching.Grains;

using System;
using System.Threading.Tasks;
using global::Orleans;
using global::Orleans.Runtime;
using Microsoft.Extensions.Logging;

/// <summary>
/// Orleans grain implementation for distributed cache entry storage.
/// Uses POCO pattern with IGrainBase for modern Orleans 7.0+ compatibility.
/// </summary>
internal sealed class CacheEntryGrain : IGrainBase, ICacheEntryGrain
{
    /// <summary>
    /// Gets the grain context required by IGrainBase.
    /// </summary>
    public IGrainContext GrainContext { get; }

    private ILogger<CacheEntryGrain> Logger { get; }
    private TimeProvider TimeProvider { get; }
    private IPersistentState<CacheEntryState> State { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheEntryGrain"/> class.
    /// </summary>
    /// <param name="grainContext">The grain context.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="timeProvider">The time provider for deterministic time handling.</param>
    /// <param name="state">The persistent state for the cache entry.</param>
    public CacheEntryGrain(
        IGrainContext grainContext,
        ILogger<CacheEntryGrain> logger,
        TimeProvider timeProvider,
        [PersistentState("cacheEntry", "CacheStorage")]
        IPersistentState<CacheEntryState> state)
    {
        GrainContext = grainContext;
        Logger = logger;
        TimeProvider = timeProvider;
        State = state;
    }

    /// <inheritdoc/>
    public Task<CacheEntryData?> GetAsync()
    {
        if (!State.RecordExists || State.State.Data is null)
        {
            return Task.FromResult<CacheEntryData?>(null);
        }

        DateTimeOffset now = TimeProvider.GetUtcNow();
        CacheEntryData data = State.State.Data;

        // Check absolute expiration
        if (data.AbsoluteExpiration.HasValue && data.AbsoluteExpiration.Value <= now)
        {
            CacheEntryGrainLoggerExtensions.CacheEntryExpired(Logger, this.GetPrimaryKeyString(), "absolute");
            return Task.FromResult<CacheEntryData?>(null);
        }

        // Check sliding expiration
        if (data.SlidingExpiration.HasValue)
        {
            DateTimeOffset expiresAt = data.LastAccessTime + data.SlidingExpiration.Value;
            if (expiresAt <= now)
            {
                CacheEntryGrainLoggerExtensions.CacheEntryExpired(Logger, this.GetPrimaryKeyString(), "sliding");
                return Task.FromResult<CacheEntryData?>(null);
            }
        }

        CacheEntryGrainLoggerExtensions.CacheEntryRetrieved(Logger, this.GetPrimaryKeyString(), data.Value.Length);
        return Task.FromResult<CacheEntryData?>(data);
    }

    /// <inheritdoc/>
    public async Task SetAsync(CacheEntryData data)
    {
        State.State = new CacheEntryState { Data = data };
        await State.WriteStateAsync();
        CacheEntryGrainLoggerExtensions.CacheEntrySet(Logger, this.GetPrimaryKeyString(), data.Value.Length);
    }

    /// <inheritdoc/>
    public async Task RemoveAsync()
    {
        await State.ClearStateAsync();
        CacheEntryGrainLoggerExtensions.CacheEntryRemoved(Logger, this.GetPrimaryKeyString());
    }

    /// <inheritdoc/>
    public async Task RefreshAsync()
    {
        if (!State.RecordExists || State.State.Data is null)
        {
            return;
        }

        CacheEntryData data = State.State.Data;
        if (data.SlidingExpiration.HasValue)
        {
            State.State.Data = data with { LastAccessTime = TimeProvider.GetUtcNow() };
            await State.WriteStateAsync();
            CacheEntryGrainLoggerExtensions.CacheEntryRefreshed(Logger, this.GetPrimaryKeyString());
        }
    }

    [GenerateSerializer]
    [Alias("Mississippi.AspNetCore.Orleans.Caching.CacheEntryState")]
    internal sealed record CacheEntryState
    {
        [Id(0)]
        public CacheEntryData? Data { get; set; }
    }
}

/// <summary>
/// High-performance logger extensions for cache entry grain operations.
/// </summary>
internal static class CacheEntryGrainLoggerExtensions
{
    private static readonly Action<ILogger, string, int, Exception?> CacheEntryRetrievedMessage =
        LoggerMessage.Define<string, int>(
            LogLevel.Debug,
            new EventId(1, nameof(CacheEntryRetrieved)),
            "Cache entry retrieved: Key={Key}, Size={Size}");

    private static readonly Action<ILogger, string, int, Exception?> CacheEntrySetMessage =
        LoggerMessage.Define<string, int>(
            LogLevel.Debug,
            new EventId(2, nameof(CacheEntrySet)),
            "Cache entry set: Key={Key}, Size={Size}");

    private static readonly Action<ILogger, string, Exception?> CacheEntryRemovedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(3, nameof(CacheEntryRemoved)),
            "Cache entry removed: Key={Key}");

    private static readonly Action<ILogger, string, Exception?> CacheEntryRefreshedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(4, nameof(CacheEntryRefreshed)),
            "Cache entry refreshed: Key={Key}");

    private static readonly Action<ILogger, string, string, Exception?> CacheEntryExpiredMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(5, nameof(CacheEntryExpired)),
            "Cache entry expired: Key={Key}, Reason={Reason}");

    /// <summary>
    /// Logs that a cache entry was retrieved.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="key">The cache entry key.</param>
    /// <param name="size">The size of the cache entry in bytes.</param>
    public static void CacheEntryRetrieved(this ILogger<CacheEntryGrain> logger, string key, int size) =>
        CacheEntryRetrievedMessage(logger, key, size, null);

    /// <summary>
    /// Logs that a cache entry was set.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="key">The cache entry key.</param>
    /// <param name="size">The size of the cache entry in bytes.</param>
    public static void CacheEntrySet(this ILogger<CacheEntryGrain> logger, string key, int size) =>
        CacheEntrySetMessage(logger, key, size, null);

    /// <summary>
    /// Logs that a cache entry was removed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="key">The cache entry key.</param>
    public static void CacheEntryRemoved(this ILogger<CacheEntryGrain> logger, string key) =>
        CacheEntryRemovedMessage(logger, key, null);

    /// <summary>
    /// Logs that a cache entry was refreshed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="key">The cache entry key.</param>
    public static void CacheEntryRefreshed(this ILogger<CacheEntryGrain> logger, string key) =>
        CacheEntryRefreshedMessage(logger, key, null);

    /// <summary>
    /// Logs that a cache entry expired.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="key">The cache entry key.</param>
    /// <param name="reason">The reason for expiration.</param>
    public static void CacheEntryExpired(this ILogger<CacheEntryGrain> logger, string key, string reason) =>
        CacheEntryExpiredMessage(logger, key, reason, null);
}
