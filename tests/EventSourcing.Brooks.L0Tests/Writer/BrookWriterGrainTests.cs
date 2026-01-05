using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Cursor;
using Mississippi.EventSourcing.Brooks.L0Tests.Infrastructure;
using Mississippi.EventSourcing.Brooks.Writer;
using Mississippi.Testing.Utilities.Orleans;

using Orleans.TestingHost;


namespace Mississippi.EventSourcing.Brooks.L0Tests.Writer;

/// <summary>
///     Integration tests for <see cref="IBrookWriterGrain" />.
/// </summary>
[Collection(ClusterTestSuite.Name)]
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Brooks")]
[AllureSubSuite("Brook Writer Grain")]
public sealed class BrookWriterGrainTests
{
    private readonly TestCluster cluster = TestClusterAccess.Cluster;

    /// <summary>
    ///     Appending events should publish a cursor update visible to the cursor grain.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task AppendEventsAsyncPublishesCursorUpdate()
    {
        BrookKey key = new("t", "w1");
        IBrookWriterGrain writer = cluster.GrainFactory.GetGrain<IBrookWriterGrain>(key);
        IBrookCursorGrain cursor = cluster.GrainFactory.GetGrain<IBrookCursorGrain>(key);
        ImmutableArray<BrookEvent> events =
        [
            new()
            {
                Id = "1",
            },
            new()
            {
                Id = "2",
            },
        ];
        BrookPosition newPos = await writer.AppendEventsAsync(events);

        // Cursor represents last written position (0-based), so 2 events means position 1
        Assert.Equal(1, newPos.Value);

        // Confirm cursor by reading storage-backed path
        BrookPosition confirmed = await cursor.GetLatestPositionConfirmedAsync();
        Assert.Equal(1, confirmed.Value);
    }

    /// <summary>
    ///     Appending with a stale expected version should throw.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task AppendEventsAsyncRespectsExpectedVersion()
    {
        BrookKey key = new("t", "w2");
        IBrookWriterGrain writer = cluster.GrainFactory.GetGrain<IBrookWriterGrain>(key);
        ImmutableArray<BrookEvent> e1 =
        [
            new()
            {
                Id = "1",
            },
        ];

        // Empty brook has cursor -1, so expected version for first write is -1
        // After writing 1 event, cursor becomes 0 (last written position)
        BrookPosition pos1 = await writer.AppendEventsAsync(e1, -1);
        Assert.Equal(0, pos1.Value);
        ImmutableArray<BrookEvent> e2 =
        [
            new()
            {
                Id = "2",
            },
        ];

        // Trying to append with stale expected version (-1) should throw
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await writer.AppendEventsAsync(e2, -1));
    }
}