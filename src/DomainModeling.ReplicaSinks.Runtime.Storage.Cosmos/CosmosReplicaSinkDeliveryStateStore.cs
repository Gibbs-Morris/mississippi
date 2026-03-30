using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos;

/// <summary>
///     Aggregates per-sink Cosmos durable-state shards behind the runtime's single delivery-state store contract.
/// </summary>
internal sealed class CosmosReplicaSinkDeliveryStateStore : IReplicaSinkDeliveryStateStore
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CosmosReplicaSinkDeliveryStateStore" /> class.
    /// </summary>
    /// <param name="shards">The registered per-sink Cosmos shards.</param>
    /// <param name="logger">The store logger.</param>
    public CosmosReplicaSinkDeliveryStateStore(
        IEnumerable<ICosmosReplicaSinkShard> shards,
        ILogger<CosmosReplicaSinkDeliveryStateStore> logger
    )
    {
        ArgumentNullException.ThrowIfNull(shards);
        ArgumentNullException.ThrowIfNull(logger);
        ICosmosReplicaSinkShard[] shardArray = shards.ToArray();
        string[] duplicateKeys = shardArray
            .GroupBy(static shard => shard.SinkKey, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .ToArray();
        if (duplicateKeys.Length > 0)
        {
            throw new InvalidOperationException(
                $"Duplicate Cosmos replica sink shards were registered: {string.Join(", ", duplicateKeys)}.");
        }

        Shards = shardArray.ToDictionary(static shard => shard.SinkKey, StringComparer.Ordinal);
        Logger = logger;
    }

    private ILogger<CosmosReplicaSinkDeliveryStateStore> Logger { get; }

    private IReadOnlyDictionary<string, ICosmosReplicaSinkShard> Shards { get; }

    /// <inheritdoc />
    public async Task<ReplicaSinkDeliveryStatePage> ReadDeadLetterPageAsync(
        int pageSize,
        string? continuationToken = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegative(pageSize);
        if (pageSize == 0)
        {
            return new(Array.Empty<ReplicaSinkDeliveryState>());
        }

        int offset = ParseContinuationOffset(continuationToken);
        int requestedCount = checked(offset + pageSize + 1);
        Logger.LogReadingDeadLetters(Shards.Count, offset, pageSize);
        IReadOnlyList<ReplicaSinkDeliveryState>[] shardPages = await Task.WhenAll(
                Shards.Values.Select(shard => shard.ReadDeadLettersAsync(requestedCount, cancellationToken)))
            .ConfigureAwait(false);
        List<ReplicaSinkDeliveryState> merged = shardPages
            .SelectMany(static page => page)
            .Where(static state => state.DeadLetter is not null)
            .OrderByDescending(static state => state.DeadLetter!.RecordedAtUtc)
            .ThenBy(static state => state.DeliveryKey, StringComparer.Ordinal)
            .ToList();
        List<ReplicaSinkDeliveryState> pageItems = merged.Skip(offset).Take(pageSize).ToList();
        bool hasMore = merged.Count > (offset + pageItems.Count);
        string? nextToken = hasMore && pageItems.Count > 0
            ? (offset + pageItems.Count).ToString(CultureInfo.InvariantCulture)
            : null;
        Logger.LogReadDeadLetters(pageItems.Count, hasMore);
        return new(pageItems, nextToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReplicaSinkDeliveryState>> ReadDueRetriesAsync(
        DateTimeOffset dueAtOrBeforeUtc,
        int maxCount,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxCount);
        if (maxCount == 0)
        {
            return Array.Empty<ReplicaSinkDeliveryState>();
        }

        Logger.LogReadingDueRetries(
            Shards.Count,
            dueAtOrBeforeUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            maxCount);
        IReadOnlyList<ReplicaSinkDeliveryState>[] shardResults = await Task.WhenAll(
                Shards.Values.Select(shard => shard.ReadDueRetriesAsync(dueAtOrBeforeUtc, maxCount, cancellationToken)))
            .ConfigureAwait(false);
        ReplicaSinkDeliveryState[] merged = shardResults
            .SelectMany(static states => states)
            .Where(static state => state.Retry?.NextRetryAtUtc is not null)
            .OrderBy(static state => state.Retry!.NextRetryAtUtc!.Value)
            .ThenBy(static state => state.DeliveryKey, StringComparer.Ordinal)
            .Take(maxCount)
            .ToArray();
        Logger.LogReadDueRetries(merged.Length);
        return merged;
    }

    /// <inheritdoc />
    public async Task<ReplicaSinkDeliveryState?> ReadAsync(
        string deliveryKey,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deliveryKey);
        string sinkKey = ParseSinkKey(deliveryKey);
        if (!Shards.TryGetValue(sinkKey, out ICosmosReplicaSinkShard? shard))
        {
            return null;
        }

        Logger.LogReadingState(deliveryKey, sinkKey);
        return await shard.ReadStateAsync(deliveryKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task WriteAsync(
        ReplicaSinkDeliveryState state,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        string sinkKey = ParseSinkKey(state.DeliveryKey);
        ICosmosReplicaSinkShard shard = GetRequiredShard(sinkKey, state.DeliveryKey);
        Logger.LogWritingState(state.DeliveryKey, sinkKey);
        await shard.WriteStateAsync(state, cancellationToken).ConfigureAwait(false);
    }

    private ICosmosReplicaSinkShard GetRequiredShard(
        string sinkKey,
        string deliveryKey
    ) => Shards.TryGetValue(sinkKey, out ICosmosReplicaSinkShard? shard)
        ? shard
        : throw new InvalidOperationException(
            $"No Cosmos replica sink registration exists for delivery key '{deliveryKey}' routed to sink '{sinkKey}'.");

    private static int ParseContinuationOffset(
        string? continuationToken
    )
    {
        if (string.IsNullOrWhiteSpace(continuationToken))
        {
            return 0;
        }

        if (!int.TryParse(continuationToken, NumberStyles.None, CultureInfo.InvariantCulture, out int offset) || offset < 0)
        {
            throw new ArgumentException(
                $"Continuation token '{continuationToken}' is not a valid Cosmos replica sink dead-letter offset.",
                nameof(continuationToken));
        }

        return offset;
    }

    private static string ParseSinkKey(
        string deliveryKey
    )
    {
        string[] parts = deliveryKey.Split("::", 4, StringSplitOptions.None);
        if (parts.Length != 4 || string.IsNullOrWhiteSpace(parts[1]))
        {
            throw new InvalidOperationException(
                $"Delivery key '{deliveryKey}' is not a valid runtime replica sink delivery key.");
        }

        return parts[1];
    }
}
