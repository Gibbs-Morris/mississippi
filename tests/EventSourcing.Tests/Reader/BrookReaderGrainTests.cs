using System.Collections.Immutable;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Reader;
using Mississippi.EventSourcing.Tests.Infrastructure;
using Mississippi.EventSourcing.Writer;

using Orleans.TestingHost;


namespace Mississippi.EventSourcing.Tests.Reader;

[Collection(ClusterCollection.Name)]
public class BrookReaderGrainTests(ClusterFixture fixture)
{
    private readonly TestCluster cluster = fixture.Cluster;

    [Fact]
    public async Task ReadEventsAsync_SlicesByOptionsAndReturnsAll()
    {
        BrookKey key = new("t", "reader1");
        IBrookWriterGrain writer = cluster.GrainFactory.GetGrain<IBrookWriterGrain>(key);
        // default is 100; test pipeline uses internal reading logic anyway; seed 5 events
        ImmutableArray<BrookEvent> batch = Enumerable.Range(0, 5)
            .Select(i => new BrookEvent
            {
                Id = i.ToString(),
            })
            .ToImmutableArray();
        await writer.AppendEventsAsync(batch);
        // Ensure head cache has advanced before full reader walk
        var head = cluster.GrainFactory.GetGrain<Mississippi.EventSourcing.Head.IBrookHeadGrain>(key);
        await head.GetLatestPositionConfirmedAsync();
        IBrookReaderGrain reader = cluster.GrainFactory.GetGrain<IBrookReaderGrain>(key);
        List<BrookEvent> got = new();
        await foreach (BrookEvent e in reader.ReadEventsAsync(0, 4))
        {
            got.Add(e);
        }

        Assert.Equal(["0", "1", "2", "3", "4"], got.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task ReadEventsBatchAsync_ReturnsImmutableArray()
    {
        BrookKey key = new("t", "reader2");
        IBrookWriterGrain writer = cluster.GrainFactory.GetGrain<IBrookWriterGrain>(key);
        await writer.AppendEventsAsync(
        [
            new()
            {
                Id = "a",
            },
        ]);
        // Ensure head advanced
        var head2 = cluster.GrainFactory.GetGrain<Mississippi.EventSourcing.Head.IBrookHeadGrain>(key);
        await head2.GetLatestPositionConfirmedAsync();
        IBrookReaderGrain reader = cluster.GrainFactory.GetGrain<IBrookReaderGrain>(key);
        ImmutableArray<BrookEvent> batch = await reader.ReadEventsBatchAsync(0, null);
        Assert.Single(batch);
        Assert.Equal("a", batch[0].Id);
    }
}