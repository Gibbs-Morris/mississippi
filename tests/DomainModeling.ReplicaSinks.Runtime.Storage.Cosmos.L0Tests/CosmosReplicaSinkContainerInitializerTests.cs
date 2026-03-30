using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos;


namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.L0Tests;

/// <summary>
///     Tests the hosted Cosmos container initializer.
/// </summary>
public sealed class CosmosReplicaSinkContainerInitializerTests
{
    private sealed class FakeShard : ICosmosReplicaSinkShard
    {
        private readonly Exception? ensureException;

        public FakeShard(
            string sinkKey,
            Exception? ensureException = null
        )
        {
            SinkKey = sinkKey;
            this.ensureException = ensureException;
        }

        public int EnsureContainerCallCount { get; private set; }

        public string SinkKey { get; }

        public Task EnsureContainerAsync(
            CancellationToken cancellationToken = default
        )
        {
            EnsureContainerCallCount++;
            return ensureException is null ? Task.CompletedTask : Task.FromException(ensureException);
        }

        public Task<IReadOnlyList<ReplicaSinkDeliveryState>> ReadDeadLettersAsync(
            int maxCount,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult<IReadOnlyList<ReplicaSinkDeliveryState>>(Array.Empty<ReplicaSinkDeliveryState>());

        public Task<IReadOnlyList<ReplicaSinkDeliveryState>> ReadDueRetriesAsync(
            DateTimeOffset dueAtOrBeforeUtc,
            int maxCount,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult<IReadOnlyList<ReplicaSinkDeliveryState>>(Array.Empty<ReplicaSinkDeliveryState>());

        public Task<ReplicaSinkDeliveryState?> ReadStateAsync(
            string deliveryKey,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult<ReplicaSinkDeliveryState?>(null);

        public Task WriteStateAsync(
            ReplicaSinkDeliveryState state,
            CancellationToken cancellationToken = default
        ) =>
            Task.CompletedTask;
    }

    /// <summary>
    ///     Ensures startup validation touches every registered shard exactly once.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CosmosReplicaSinkContainerInitializerShouldEnsureEveryRegisteredShard()
    {
        FakeShard east = new("east");
        FakeShard west = new("west");
        CosmosReplicaSinkContainerInitializer initializer = new(
            [east, west],
            NullLogger<CosmosReplicaSinkContainerInitializer>.Instance);

        await initializer.StartAsync(CancellationToken.None);

        Assert.Equal(1, east.EnsureContainerCallCount);
        Assert.Equal(1, west.EnsureContainerCallCount);
    }

    /// <summary>
    ///     Ensures startup stops at the failing shard and propagates the failure.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CosmosReplicaSinkContainerInitializerShouldStopAtTheFirstFailingShard()
    {
        FakeShard first = new("first");
        FakeShard failing = new("failing", new InvalidOperationException("boom"));
        FakeShard neverReached = new("never-reached");
        CosmosReplicaSinkContainerInitializer initializer = new(
            [first, failing, neverReached],
            NullLogger<CosmosReplicaSinkContainerInitializer>.Instance);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await initializer.StartAsync(CancellationToken.None));

        Assert.Equal("boom", exception.Message);
        Assert.Equal(1, first.EnsureContainerCallCount);
        Assert.Equal(1, failing.EnsureContainerCallCount);
        Assert.Equal(0, neverReached.EnsureContainerCallCount);
    }
}
