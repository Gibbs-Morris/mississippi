using System.Collections.Immutable;
using System.Globalization;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Head;
using Mississippi.EventSourcing.Reader;
using Mississippi.EventSourcing.Tests.Infrastructure;
using Mississippi.EventSourcing.Writer;

using Orleans.TestingHost;


namespace Mississippi.EventSourcing.Tests.Reader;

/// <summary>
///     Integration tests for <see cref="IBrookSliceReaderGrain" />.
/// </summary>
[Collection(ClusterTestSuite.Name)]
public class BrookSliceReaderGrainTests
{
    private readonly TestCluster cluster = TestClusterAccess.Cluster;

    /// <summary>
    ///     Verifies slice reader populates cache and respects requested range.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task ReadAsyncPopulatesCacheAndRespectsRange()
    {
        BrookKey key = new("t", "slice1");

        // Seed events via writer to fill storage and head positions
        IBrookWriterGrain writer = cluster.GrainFactory.GetGrain<IBrookWriterGrain>(key);
        ImmutableArray<BrookEvent> batch = Enumerable.Range(0, 10)
            .Select(i => new BrookEvent
            {
                Id = i.ToString(CultureInfo.InvariantCulture),
            })
            .ToImmutableArray();
        await writer.AppendEventsAsync(batch);

        // Ensure head cache has advanced before slice read
        IBrookHeadGrain head = cluster.GrainFactory.GetGrain<IBrookHeadGrain>(key);
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

    /// <summary>
    ///     Verifies batch slice read returns expected immutable array.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task ReadBatchAsyncReturnsImmutableArray()
    {
        BrookKey key = new("t", "slice2");
        IBrookWriterGrain writer = cluster.GrainFactory.GetGrain<IBrookWriterGrain>(key);
        ImmutableArray<BrookEvent> batch = Enumerable.Range(0, 5)
            .Select(i => new BrookEvent
            {
                Id = i.ToString(CultureInfo.InvariantCulture),
            })
            .ToImmutableArray();
        await writer.AppendEventsAsync(batch);
        IBrookHeadGrain head = cluster.GrainFactory.GetGrain<IBrookHeadGrain>(key);
        await head.GetLatestPositionConfirmedAsync();
        BrookRangeKey sliceKey = BrookRangeKey.FromBrookCompositeKey(key, 0, 5);
        IBrookSliceReaderGrain slice = cluster.GrainFactory.GetGrain<IBrookSliceReaderGrain>(sliceKey);
        ImmutableArray<BrookEvent> got = await slice.ReadBatchAsync(1, 3);
        Assert.Equal(["1", "2", "3"], got.Select(e => e.Id).ToArray());
    }
}
