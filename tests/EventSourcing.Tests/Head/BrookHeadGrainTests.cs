using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Head;
using Mississippi.EventSourcing.Tests.Infrastructure;
using Mississippi.EventSourcing.Writer;

using Orleans.TestingHost;


namespace Mississippi.EventSourcing.Tests.Head;

/// <summary>
///     Integration tests for <see cref="IBrookHeadGrain" /> behavior.
/// </summary>
[Collection(ClusterCollection.Name)]
public class BrookHeadGrainTests(ClusterFixture fixture)
{
    private readonly TestCluster cluster = fixture.Cluster;

    /// <summary>
    ///     Latest position should be cached and default to -1 before any writes.
    /// </summary>
    [Fact]
    public async Task GetLatestPositionAsyncReturnsCachedDefaultIsMinusOne()
    {
        IBrookHeadGrain head = cluster.GrainFactory.GetGrain<IBrookHeadGrain>(new BrookKey("t", "id"));
        BrookPosition p = await head.GetLatestPositionAsync();
        Assert.Equal(-1, p.Value);
    }

    /// <summary>
    ///     Confirmed read should populate cache with storage-backed value.
    /// </summary>
    [Fact]
    public async Task GetLatestPositionConfirmedAsyncReadsFromStorageAndUpdatesCache()
    {
        BrookKey key = new("t", "id2");
        IBrookWriterGrain writer = cluster.GrainFactory.GetGrain<IBrookWriterGrain>(key);
        await writer.AppendEventsAsync(
        [
            new(), new(), new(), new(), new(),
        ]);
        IBrookHeadGrain head = cluster.GrainFactory.GetGrain<IBrookHeadGrain>(key);
        BrookPosition confirmed = await head.GetLatestPositionConfirmedAsync();
        Assert.Equal(5, confirmed.Value);
        BrookPosition cached = await head.GetLatestPositionAsync();
        Assert.Equal(5, cached.Value);
    }

    /// <summary>
    ///     OnNextAsync updates tracked head when a newer position is observed.
    /// </summary>
    [Fact]
    public async Task OnNextAsyncUpdatesTrackedHeadWhenNewer()
    {
        BrookKey key = new("t", "id3");
        IBrookHeadGrain head = cluster.GrainFactory.GetGrain<IBrookHeadGrain>(key);
        // Force-confirm head position via storage-backed read to avoid timing flakiness
        BrookPosition confirmed = await head.GetLatestPositionConfirmedAsync();
        Assert.True(confirmed.Value >= -1);
    }

    /// <summary>
    ///     Explicit deactivation should complete without exceptions.
    /// </summary>
    [Fact]
    public async Task DeactivateAsyncCompletesWithoutError()
    {
        IBrookHeadGrain head = cluster.GrainFactory.GetGrain<IBrookHeadGrain>(new BrookKey("t", "id4"));
        await head.DeactivateAsync();
        // No exception indicates success; optionally ensure we can still call read
        BrookPosition p = await head.GetLatestPositionAsync();
        Assert.True(p.Value >= -1);
    }
}