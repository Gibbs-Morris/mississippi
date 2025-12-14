using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.AspNetCore.Orleans.OutputCaching.Grains;
using Mississippi.AspNetCore.Orleans.OutputCaching.Options;

using Orleans;


namespace Mississippi.AspNetCore.Orleans.OutputCaching;

/// <summary>
///     Orleans-backed implementation of <see cref="IOutputCacheStore" />.
/// </summary>
public sealed class OrleansOutputCacheStore : IOutputCacheStore
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="OrleansOutputCacheStore" /> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="clusterClient">The Orleans cluster client.</param>
    /// <param name="options">The output cache options.</param>
    /// <param name="timeProvider">The time provider.</param>
    public OrleansOutputCacheStore(
        ILogger<OrleansOutputCacheStore> logger,
        IClusterClient clusterClient,
        IOptions<OrleansOutputCacheOptions> options,
        TimeProvider timeProvider
    )
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ClusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    private IClusterClient ClusterClient { get; }

    private ILogger<OrleansOutputCacheStore> Logger { get; }

    private IOptions<OrleansOutputCacheOptions> Options { get; }

    private TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public async ValueTask EvictByTagAsync(
        string tag,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(tag);
        cancellationToken.ThrowIfCancellationRequested();

        // Get all keys associated with this tag from the tag index
        ITagIndexGrain tagIndexGrain = ClusterClient.GetGrain<ITagIndexGrain>(tag);
        IReadOnlyList<string> keys = await tagIndexGrain.GetKeysAsync();

        // Evict each entry that has this tag
        foreach (string key in keys)
        {
            IOutputCacheEntryGrain grain = ClusterClient.GetGrain<IOutputCacheEntryGrain>(key);
            await grain.EvictAsync();
        }

        Logger.TagEvicted(tag, keys.Count);
    }

    /// <inheritdoc />
    public async ValueTask<byte[]?> GetAsync(
        string key,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        cancellationToken.ThrowIfCancellationRequested();
        string grainKey = GetGrainKey(key);
        IOutputCacheEntryGrain grain = ClusterClient.GetGrain<IOutputCacheEntryGrain>(grainKey);
        OutputCacheEntryData? data = await grain.GetAsync();
        return data?.Body;
    }

    /// <inheritdoc />
    public async ValueTask SetAsync(
        string key,
        byte[] value,
        string[]? tags,
        TimeSpan validFor,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        cancellationToken.ThrowIfCancellationRequested();
        DateTimeOffset expiresAt = TimeProvider.GetUtcNow() + validFor;
        OutputCacheEntryData data = new()
        {
            Body = value,
            ExpiresAt = expiresAt,
            Tags = tags ?? [],
        };
        string grainKey = GetGrainKey(key);
        IOutputCacheEntryGrain grain = ClusterClient.GetGrain<IOutputCacheEntryGrain>(grainKey);
        await grain.SetAsync(data, tags);

        // Update tag index for each tag
        if (tags is { Length: > 0 })
        {
            foreach (string tag in tags)
            {
                ITagIndexGrain tagIndexGrain = ClusterClient.GetGrain<ITagIndexGrain>(tag);
                await tagIndexGrain.AddKeyAsync(grainKey);
            }
        }
    }

    private string GetGrainKey(
        string key
    ) =>
        $"{Options.Value.KeyPrefix}:{key}";
}

/// <summary>
///     Logger extensions for <see cref="OrleansOutputCacheStore" />.
/// </summary>
internal static class OutputCacheStoreLoggerExtensions
{
    private static readonly Action<ILogger, string, int, Exception?> TagEvictedMessage =
        LoggerMessage.Define<string, int>(
            LogLevel.Debug,
            new(1, nameof(TagEvicted)),
            "Evicted {Count} entries with tag '{Tag}'");

    /// <summary>
    ///     Logs that entries were evicted by tag.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="tag">The tag for eviction.</param>
    /// <param name="count">The number of entries evicted.</param>
    public static void TagEvicted(
        this ILogger<OrleansOutputCacheStore> logger,
        string tag,
        int count
    ) =>
        TagEvictedMessage(logger, tag, count, null);
}