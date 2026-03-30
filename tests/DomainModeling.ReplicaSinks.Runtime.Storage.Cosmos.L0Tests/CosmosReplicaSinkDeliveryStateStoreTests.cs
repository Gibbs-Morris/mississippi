using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos;


namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.L0Tests;

/// <summary>
///     Tests the aggregate Cosmos delivery-state store.
/// </summary>
public sealed class CosmosReplicaSinkDeliveryStateStoreTests
{
    /// <summary>
    ///     Ensures state reads and writes route to the correct sink shard.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CosmosReplicaSinkDeliveryStateStoreShouldRouteStateReadsAndWritesBySinkKey()
    {
        FakeShard east = new("east");
        FakeShard west = new("west");
        CosmosReplicaSinkDeliveryStateStore store = new([east, west], NullLogger<CosmosReplicaSinkDeliveryStateStore>.Instance);
        ReplicaSinkDeliveryState eastState = new("Projection::east::orders-read::1", 10, null, 9);
        ReplicaSinkDeliveryState westState = new("Projection::west::orders-read::2", 20, null, 19);

        await store.WriteAsync(eastState, CancellationToken.None);
        await store.WriteAsync(westState, CancellationToken.None);

        ReplicaSinkDeliveryState? roundTrippedEast = await store.ReadAsync(eastState.DeliveryKey, CancellationToken.None);
        ReplicaSinkDeliveryState? roundTrippedWest = await store.ReadAsync(westState.DeliveryKey, CancellationToken.None);

        Assert.Equal(eastState.DeliveryKey, roundTrippedEast?.DeliveryKey);
        Assert.Equal(westState.DeliveryKey, roundTrippedWest?.DeliveryKey);
        Assert.Single(east.StoredStates);
        Assert.Single(west.StoredStates);
    }

    /// <summary>
    ///     Ensures due retries merge across shards in due-time order.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CosmosReplicaSinkDeliveryStateStoreShouldMergeDueRetriesAcrossShards()
    {
        FakeShard east = new("east");
        FakeShard west = new("west");
        east.Seed(
            new ReplicaSinkDeliveryState(
                "Projection::east::orders-read::1",
                10,
                null,
                null,
                new ReplicaSinkStoredFailure(
                    10,
                    1,
                    "retry-east-1",
                    "Retry east 1",
                    new(2026, 3, 30, 12, 0, 0, TimeSpan.Zero),
                    new(2026, 3, 30, 12, 5, 0, TimeSpan.Zero))),
            new ReplicaSinkDeliveryState(
                "Projection::east::orders-read::2",
                11,
                null,
                null,
                new ReplicaSinkStoredFailure(
                    11,
                    1,
                    "retry-east-2",
                    "Retry east 2",
                    new(2026, 3, 30, 12, 0, 0, TimeSpan.Zero),
                    new(2026, 3, 30, 12, 7, 0, TimeSpan.Zero))));
        west.Seed(
            new ReplicaSinkDeliveryState(
                "Projection::west::orders-read::1",
                12,
                null,
                null,
                new ReplicaSinkStoredFailure(
                    12,
                    1,
                    "retry-west-1",
                    "Retry west 1",
                    new(2026, 3, 30, 12, 0, 0, TimeSpan.Zero),
                    new(2026, 3, 30, 12, 4, 0, TimeSpan.Zero))));
        CosmosReplicaSinkDeliveryStateStore store = new([east, west], NullLogger<CosmosReplicaSinkDeliveryStateStore>.Instance);

        IReadOnlyList<ReplicaSinkDeliveryState> due = await store.ReadDueRetriesAsync(
            new(2026, 3, 30, 12, 5, 0, TimeSpan.Zero),
            2,
            CancellationToken.None);

        Assert.Equal(
            ["Projection::west::orders-read::1", "Projection::east::orders-read::1"],
            due.Select(static state => state.DeliveryKey).ToArray());
    }

    /// <summary>
    ///     Ensures dead-letter paging merges newest-first items across shards.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CosmosReplicaSinkDeliveryStateStoreShouldPageDeadLettersAcrossShards()
    {
        FakeShard east = new("east");
        FakeShard west = new("west");
        east.Seed(
            new ReplicaSinkDeliveryState(
                "Projection::east::orders-read::1",
                10,
                deadLetter: new ReplicaSinkStoredFailure(
                    10,
                    1,
                    "dead-east-1",
                    "Dead east 1",
                    new(2026, 3, 30, 12, 1, 0, TimeSpan.Zero))),
            new ReplicaSinkDeliveryState(
                "Projection::east::orders-read::2",
                11,
                deadLetter: new ReplicaSinkStoredFailure(
                    11,
                    1,
                    "dead-east-2",
                    "Dead east 2",
                    new(2026, 3, 30, 12, 3, 0, TimeSpan.Zero))));
        west.Seed(
            new ReplicaSinkDeliveryState(
                "Projection::west::orders-read::1",
                12,
                deadLetter: new ReplicaSinkStoredFailure(
                    12,
                    1,
                    "dead-west-1",
                    "Dead west 1",
                    new(2026, 3, 30, 12, 2, 0, TimeSpan.Zero))));
        CosmosReplicaSinkDeliveryStateStore store = new([east, west], NullLogger<CosmosReplicaSinkDeliveryStateStore>.Instance);

        ReplicaSinkDeliveryStatePage firstPage = await store.ReadDeadLetterPageAsync(2, null, CancellationToken.None);
        ReplicaSinkDeliveryStatePage secondPage = await store.ReadDeadLetterPageAsync(
            2,
            firstPage.ContinuationToken,
            CancellationToken.None);

        Assert.Equal(
            ["Projection::east::orders-read::2", "Projection::west::orders-read::1"],
            firstPage.Items.Select(static state => state.DeliveryKey).ToArray());
        Assert.NotNull(firstPage.ContinuationToken);
        Assert.Single(secondPage.Items);
        Assert.Equal("Projection::east::orders-read::1", secondPage.Items[0].DeliveryKey);
        Assert.Null(secondPage.ContinuationToken);
    }

    private sealed class FakeShard : ICosmosReplicaSinkShard
    {
        public FakeShard(
            string sinkKey
        ) => SinkKey = sinkKey;

        public string SinkKey { get; }

        public Dictionary<string, ReplicaSinkDeliveryState> StoredStates { get; } = new(StringComparer.Ordinal);

        public Task EnsureContainerAsync(
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;

        public Task<IReadOnlyList<ReplicaSinkDeliveryState>> ReadDeadLettersAsync(
            int maxCount,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<IReadOnlyList<ReplicaSinkDeliveryState>>(
            StoredStates.Values
                .Where(static state => state.DeadLetter is not null)
                .OrderByDescending(static state => state.DeadLetter!.RecordedAtUtc)
                .ThenBy(static state => state.DeliveryKey, StringComparer.Ordinal)
                .Take(maxCount)
                .ToArray());

        public Task<IReadOnlyList<ReplicaSinkDeliveryState>> ReadDueRetriesAsync(
            DateTimeOffset dueAtOrBeforeUtc,
            int maxCount,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<IReadOnlyList<ReplicaSinkDeliveryState>>(
            StoredStates.Values
                .Where(state => state.Retry?.NextRetryAtUtc is not null && state.Retry.NextRetryAtUtc.Value <= dueAtOrBeforeUtc)
                .OrderBy(static state => state.Retry!.NextRetryAtUtc!.Value)
                .ThenBy(static state => state.DeliveryKey, StringComparer.Ordinal)
                .Take(maxCount)
                .ToArray());

        public Task<ReplicaSinkDeliveryState?> ReadStateAsync(
            string deliveryKey,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(StoredStates.TryGetValue(deliveryKey, out ReplicaSinkDeliveryState? state) ? state : null);

        public void Seed(
            params ReplicaSinkDeliveryState[] states
        )
        {
            foreach (ReplicaSinkDeliveryState state in states)
            {
                StoredStates[state.DeliveryKey] = state;
            }
        }

        public Task WriteStateAsync(
            ReplicaSinkDeliveryState state,
            CancellationToken cancellationToken = default
        )
        {
            StoredStates[state.DeliveryKey] = state;
            return Task.CompletedTask;
        }
    }
}
