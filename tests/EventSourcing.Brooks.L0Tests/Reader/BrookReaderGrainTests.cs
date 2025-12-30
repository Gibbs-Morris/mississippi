using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Brooks.Cursor;
using Mississippi.EventSourcing.Reader;
using Mississippi.EventSourcing.Tests.Infrastructure;
using Mississippi.EventSourcing.Writer;
using Mississippi.Testing.Utilities.Orleans;

using Orleans.TestingHost;


namespace Mississippi.EventSourcing.Tests.Reader;

/// <summary>
///     Integration tests for <see cref="IBrookReaderGrain" /> and <see cref="IBrookAsyncReaderGrain" />.
/// </summary>
[Collection(ClusterTestSuite.Name)]
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Brooks")]
[AllureSubSuite("Brook Reader Grain Integration")]
public sealed class BrookReaderGrainTests
{
    private readonly TestCluster cluster = TestClusterAccess.Cluster;

    /// <summary>
    ///     Verifies range slicing and full enumeration via async reader grain's ReadEventsAsync.
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

        // Ensure cursor cache has advanced before full reader walk
        IBrookCursorGrain cursor = cluster.GrainFactory.GetGrain<IBrookCursorGrain>(key);
        await cursor.GetLatestPositionConfirmedAsync();

        // Use the async reader grain for streaming (create unique instance key)
        BrookAsyncReaderKey asyncReaderKey = BrookAsyncReaderKey.Create(key);
        IBrookAsyncReaderGrain asyncReader = cluster.GrainFactory.GetGrain<IBrookAsyncReaderGrain>(asyncReaderKey);
        List<BrookEvent> got = new();
        await foreach (BrookEvent e in asyncReader.ReadEventsAsync(0, 4))
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
        BrookKey key = new("t", $"reader2-{Guid.NewGuid():N}");
        IBrookWriterGrain writer = cluster.GrainFactory.GetGrain<IBrookWriterGrain>(key);
        await writer.AppendEventsAsync(
        [
            new()
            {
                Id = "a",
            },
        ]);

        // Ensure cursor advanced
        IBrookCursorGrain cursorGrain = cluster.GrainFactory.GetGrain<IBrookCursorGrain>(key);
        await cursorGrain.GetLatestPositionConfirmedAsync();
        IBrookReaderGrain reader = cluster.GrainFactory.GetGrain<IBrookReaderGrain>(key);
        ImmutableArray<BrookEvent> batch = await reader.ReadEventsBatchAsync(0);
        Assert.Single(batch);
        Assert.Equal("a", batch[0].Id);
    }
}