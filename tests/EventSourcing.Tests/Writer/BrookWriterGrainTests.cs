using System.Collections.Immutable;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Head;
using Mississippi.EventSourcing.Tests.Infrastructure;
using Mississippi.EventSourcing.Writer;

using Orleans.TestingHost;


namespace Mississippi.EventSourcing.Tests.Writer;

/// <summary>
///     Integration tests for <see cref="IBrookWriterGrain" />.
/// </summary>
[Collection(ClusterCollection.Name)]
public class BrookWriterGrainTests(ClusterFixture fixture)
{
    private readonly TestCluster cluster = fixture.Cluster;

    /// <summary>
    ///     Appending events should publish a head update visible to the head grain.
    /// </summary>
    [Fact]
    public async Task AppendEventsAsync_AppendsAndPublishesHeadUpdate()
    {
        BrookKey key = new("t", "w1");
        IBrookWriterGrain writer = cluster.GrainFactory.GetGrain<IBrookWriterGrain>(key);
        IBrookHeadGrain head = cluster.GrainFactory.GetGrain<IBrookHeadGrain>(key);
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
        Assert.Equal(2, newPos.Value);
        // Confirm head by reading storage-backed path
        BrookPosition confirmed = await head.GetLatestPositionConfirmedAsync();
        Assert.Equal(2, confirmed.Value);
    }

    /// <summary>
    ///     Appending with a stale expected version should throw.
    /// </summary>
    [Fact]
    public async Task AppendEventsAsync_RespectsExpectedVersion()
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
        BrookPosition pos1 = await writer.AppendEventsAsync(e1, 0);
        Assert.Equal(1, pos1.Value);
        ImmutableArray<BrookEvent> e2 =
        [
            new()
            {
                Id = "2",
            },
        ];
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await writer.AppendEventsAsync(e2, 0));
    }
}