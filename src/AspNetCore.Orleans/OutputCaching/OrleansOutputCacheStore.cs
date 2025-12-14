using System;
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
        cancellationToken.ThrowIfCancellationRequested();
        // Note: This is a simplified implementation. For production, you'd need a tag index grain
        // that tracks all keys associated with a tag for efficient eviction.
        // For now, we just log that this operation would need a more sophisticated design.
        Logger.TagEvictionNotImplemented(tag);
        await Task.CompletedTask;
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
    private static readonly Action<ILogger, string, Exception?> TagEvictionNotImplementedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new(1, nameof(TagEvictionNotImplemented)),
            "EvictByTagAsync called for tag '{Tag}' but tag-based eviction requires a tag index implementation");

    /// <summary>
    ///     Logs that tag eviction is not implemented.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="tag">The tag to evict.</param>
    public static void TagEvictionNotImplemented(
        this ILogger<OrleansOutputCacheStore> logger,
        string tag
    ) =>
        TagEvictionNotImplementedMessage(logger, tag, null);
}