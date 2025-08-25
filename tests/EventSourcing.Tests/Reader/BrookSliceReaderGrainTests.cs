using System.Collections.Immutable;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Reader;
using Mississippi.EventSourcing.Tests.Infrastructure;
using Mississippi.EventSourcing.Writer;

using Orleans.TestingHost;


namespace Mississippi.EventSourcing.Tests.Reader;

[Collection(ClusterCollection.Name)]
public class BrookSliceReaderGrainTests(ClusterFixture fixture)
{
    private readonly TestCluster cluster = fixture.Cluster;

    [Fact]
    public async Task ReadAsync_PopulatesCacheAndRespectsRange()
    {
        BrookKey key = new("t", "slice1");
        // Seed events via writer to fill storage and head positions
        IBrookWriterGrain writer = cluster.GrainFactory.GetGrain<IBrookWriterGrain>(key);
        ImmutableArray<BrookEvent> batch = Enumerable.Range(0, 10)
            .Select(i => new BrookEvent
            {
                Id = i.ToString(),
            })
            .ToImmutableArray();
        await writer.AppendEventsAsync(batch);
        // Ensure head cache has advanced before slice read
        var head = cluster.GrainFactory.GetGrain<Mississippi.EventSourcing.Head.IBrookHeadGrain>(key);
        await head.GetLatestPositionConfirmedAsync();
        BrookRangeKey sliceKey = BrookRangeKey.FromBrookCompositeKey(key, 2, 8); // covers [2..10)
        IBrookSliceReaderGrain slice = cluster.GrainFactory.GetGrain<IBrookSliceReaderGrain>(sliceKey);
        List<BrookEvent> got = new();
        await foreach (BrookEvent e in slice.ReadAsync(3, 6))
        {
            got.Add(e);
        }

        Assert.Equal(4, got.Count); // positions 3,4,5,6
        Assert.Equal(["3", "4", "5", "6"], got.Select(x => x.Id).ToArray());
    }
}