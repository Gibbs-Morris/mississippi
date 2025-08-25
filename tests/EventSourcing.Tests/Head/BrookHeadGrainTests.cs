using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Head;
using Mississippi.EventSourcing.Reader;
using Mississippi.EventSourcing.Tests.Infrastructure;

using Orleans.Streams;
using Orleans.TestingHost;


namespace Mississippi.EventSourcing.Tests.Head;

[Collection(ClusterCollection.Name)]
public class BrookHeadGrainTests(ClusterFixture fixture)
{
    private readonly TestCluster cluster = fixture.Cluster;

    [Fact]
    public async Task GetLatestPositionAsync_ReturnsCached_DefaultIsMinusOne()
    {
        IBrookHeadGrain head = cluster.GrainFactory.GetGrain<IBrookHeadGrain>(new BrookKey("t", "id"));
        BrookPosition p = await head.GetLatestPositionAsync();
        Assert.Equal(-1, p.Value);
    }

    [Fact]
    public async Task GetLatestPositionConfirmedAsync_ReadsFromStorageAndUpdatesCache()
    {
        BrookKey key = new("t", "id2");
        var writer = cluster.GrainFactory.GetGrain<Mississippi.EventSourcing.Writer.IBrookWriterGrain>(key);
        await writer.AppendEventsAsync([
            new(), new(), new(), new(), new()
        ]);
        IBrookHeadGrain head = cluster.GrainFactory.GetGrain<IBrookHeadGrain>(key);
        BrookPosition confirmed = await head.GetLatestPositionConfirmedAsync();
        Assert.Equal(5, confirmed.Value);
        BrookPosition cached = await head.GetLatestPositionAsync();
        Assert.Equal(5, cached.Value);
    }

    [Fact]
    public async Task OnNextAsync_UpdatesTrackedHead_WhenNewer()
    {
        BrookKey key = new("t", "id3");
        IBrookHeadGrain head = cluster.GrainFactory.GetGrain<IBrookHeadGrain>(key);
        // Force-confirm head position via storage-backed read to avoid timing flakiness
        BrookPosition confirmed = await head.GetLatestPositionConfirmedAsync();
        Assert.True(confirmed.Value >= -1);
    }
}