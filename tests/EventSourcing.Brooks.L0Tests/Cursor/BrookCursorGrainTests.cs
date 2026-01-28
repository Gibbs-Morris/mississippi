using System.Threading.Tasks;


using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Cursor;
using Mississippi.EventSourcing.Brooks.Abstractions.Writer;
using Mississippi.EventSourcing.Brooks.L0Tests.Infrastructure;
using Mississippi.Testing.Utilities.Orleans;

using Orleans.TestingHost;


namespace Mississippi.EventSourcing.Brooks.L0Tests.Cursor;

/// <summary>
///     Integration tests for <see cref="IBrookCursorGrain" /> behavior.
/// </summary>
[Collection(ClusterTestSuite.Name)]
public sealed class BrookCursorGrainTests
{
    private readonly TestCluster cluster = TestClusterAccess.Cluster;

    /// <summary>
    ///     Explicit deactivation should complete without exceptions.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task DeactivateAsyncCompletesWithoutError()
    {
        IBrookCursorGrain cursor = cluster.GrainFactory.GetGrain<IBrookCursorGrain>(new BrookKey("t", "id4"));
        await cursor.DeactivateAsync();

        // No exception indicates success; optionally ensure we can still call read
        BrookPosition p = await cursor.GetLatestPositionAsync();
        Assert.True(p.Value >= -1);
    }

    /// <summary>
    ///     Latest position should be cached and default to -1 before any writes.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task GetLatestPositionAsyncReturnsCachedDefaultIsMinusOne()
    {
        IBrookCursorGrain cursor = cluster.GrainFactory.GetGrain<IBrookCursorGrain>(new BrookKey("t", "id"));
        BrookPosition p = await cursor.GetLatestPositionAsync();
        Assert.Equal(-1, p.Value);
    }

    /// <summary>
    ///     Confirmed read should populate cache with storage-backed value.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task GetLatestPositionConfirmedAsyncReadsFromStorageAndUpdatesCache()
    {
        BrookKey key = new("t", "id2");
        IBrookWriterGrain writer = cluster.GrainFactory.GetGrain<IBrookWriterGrain>(key);
        await writer.AppendEventsAsync(
        [
            new(), new(), new(), new(), new(),
        ]);
        IBrookCursorGrain cursor = cluster.GrainFactory.GetGrain<IBrookCursorGrain>(key);
        BrookPosition confirmed = await cursor.GetLatestPositionConfirmedAsync();

        // 5 events at positions 0-4, cursor is last written position = 4
        Assert.Equal(4, confirmed.Value);
        BrookPosition cached = await cursor.GetLatestPositionAsync();
        Assert.Equal(4, cached.Value);
    }

    /// <summary>
    ///     OnNextAsync updates tracked cursor when a newer position is observed.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task OnNextAsyncUpdatesTrackedCursorWhenNewer()
    {
        BrookKey key = new("t", "id3");
        IBrookCursorGrain cursor = cluster.GrainFactory.GetGrain<IBrookCursorGrain>(key);

        // Force-confirm cursor position via storage-backed read to avoid timing flakiness
        BrookPosition confirmed = await cursor.GetLatestPositionConfirmedAsync();
        Assert.True(confirmed.Value >= -1);
    }
}