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
///     Integration tests for <see cref="IBrookReaderGrain" />.
/// </summary>
[Collection(ClusterCollection.Name)]
public class BrookReaderGrainTests(ClusterFixture fixture)
{
    private readonly TestCluster cluster = fixture.Cluster;

    /// <summary>
    ///     Verifies range slicing and full enumeration via ReadEventsAsync.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task ReadEventsAsyncSlicesByOptionsAndReturnsAll()
    {
        BrookKey key = new("t", "reader1");
        IBrookWriterGrain writer = cluster.GrainFactory.GetGrain<IBrookWriterGrain>(key);

        // default is 100; test pipeline uses internal reading logic anyway; seed 5 events
        ImmutableArray<BrookEvent> batch = Enumerable.Range(0, 5)
            .Select(i => new BrookEvent
            {
                Id = i.ToString(CultureInfo.InvariantCulture),
            })
            .ToImmutableArray();
        await writer.AppendEventsAsync(batch);

        // Ensure head cache has advanced before full reader walk
        IBrookHeadGrain head = cluster.GrainFactory.GetGrain<IBrookHeadGrain>(key);
        await head.GetLatestPositionConfirmedAsync();
        IBrookReaderGrain reader = cluster.GrainFactory.GetGrain<IBrookReaderGrain>(key);
        List<BrookEvent> got = new();
        await foreach (BrookEvent e in reader.ReadEventsAsync(0, 4))
        {
            got.Add(e);
        }

        Assert.Equal(["0", "1", "2", "3", "4"], got.Select(x => x.Id).ToArray());
    }

    /// <summary>
    ///     Verifies batch read returns an immutable array with expected events.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task ReadEventsBatchAsyncReturnsImmutableArray()
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
        IBrookHeadGrain head2 = cluster.GrainFactory.GetGrain<IBrookHeadGrain>(key);
        await head2.GetLatestPositionConfirmedAsync();
        IBrookReaderGrain reader = cluster.GrainFactory.GetGrain<IBrookReaderGrain>(key);
        ImmutableArray<BrookEvent> batch = await reader.ReadEventsBatchAsync(0);
        Assert.Single(batch);
        Assert.Equal("a", batch[0].Id);
    }
}