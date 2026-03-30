using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Bootstrap;

/// <summary>
///     In-memory durable-state proof store for Increment 03a latest-state processing.
/// </summary>
public sealed class BootstrapReplicaSinkDeliveryStateStore : IReplicaSinkDeliveryStateStore
{
    private ConcurrentDictionary<string, ReplicaSinkDeliveryState> States { get; } = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Task<ReplicaSinkDeliveryStatePage> ReadDeadLetterPageAsync(
        int pageSize,
        string? continuationToken = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        cancellationToken.ThrowIfCancellationRequested();
        int offset = ParseContinuationToken(continuationToken);
        List<ReplicaSinkDeliveryState> orderedDeadLetters = States.Values
            .Where(static state => state.DeadLetter is not null)
            .OrderByDescending(static state => state.DeadLetter!.RecordedAtUtc)
            .ThenBy(static state => state.DeliveryKey, StringComparer.Ordinal)
            .ToList();
        List<ReplicaSinkDeliveryState> items = orderedDeadLetters
            .Skip(offset)
            .Take(pageSize)
            .ToList();
        int nextOffset = offset + items.Count;
        bool hasMore = nextOffset < orderedDeadLetters.Count;
        return Task.FromResult(
            new ReplicaSinkDeliveryStatePage(
                items,
                hasMore ? nextOffset.ToString(CultureInfo.InvariantCulture) : null));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ReplicaSinkDeliveryState>> ReadDueRetriesAsync(
        DateTimeOffset dueAtOrBeforeUtc,
        int maxCount,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCount, 1);
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<ReplicaSinkDeliveryState> items = States.Values
            .Where(state => state.Retry?.NextRetryAtUtc is DateTimeOffset nextRetryAtUtc && nextRetryAtUtc <= dueAtOrBeforeUtc)
            .OrderBy(static state => state.Retry!.NextRetryAtUtc)
            .ThenBy(static state => state.DeliveryKey, StringComparer.Ordinal)
            .Take(maxCount)
            .ToList();
        return Task.FromResult(items);
    }

    /// <inheritdoc />
    public Task<ReplicaSinkDeliveryState?> ReadAsync(
        string deliveryKey,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deliveryKey);
        cancellationToken.ThrowIfCancellationRequested();
        _ = States.TryGetValue(deliveryKey, out ReplicaSinkDeliveryState? state);
        return Task.FromResult(state);
    }

    /// <inheritdoc />
    public Task WriteAsync(
        ReplicaSinkDeliveryState state,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        cancellationToken.ThrowIfCancellationRequested();
        States[state.DeliveryKey] = state;
        return Task.CompletedTask;
    }

    private static int ParseContinuationToken(
        string? continuationToken
    )
    {
        if (string.IsNullOrWhiteSpace(continuationToken))
        {
            return 0;
        }

        if (!int.TryParse(continuationToken, CultureInfo.InvariantCulture, out int offset) || offset < 0)
        {
            throw new InvalidOperationException("Dead-letter continuation token was invalid.");
        }

        return offset;
    }
}