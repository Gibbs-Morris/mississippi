using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Abstractions.Factory;
using Mississippi.Brooks.Abstractions.Reader;
using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime;
using Mississippi.Tributary.Runtime.L0Tests;
using Mississippi.Tributary.Runtime.Storage.Abstractions;

using Moq;

using Orleans.Runtime;


namespace MississippiTests.Tributary.Runtime.L0Tests;

/// <summary>
///     Verifies that snapshot identity follows typed reducer priority.
/// </summary>
public sealed class ReducerOrderTests
{
    private static async IAsyncEnumerable<BrookEvent> ReadEventAsync(
        BrookEvent brookEvent
    )
    {
        await Task.CompletedTask;
        yield return brookEvent;
    }

    /// <summary>
    ///     Verifies that reversing competing typed reducers changes both replay and snapshot identity.
    /// </summary>
    [Fact]
    public void CompetingReducerPriorityShouldChangeHashAndSnapshotKey()
    {
        AddingStateEventReducer adding = new();
        SubtractingStateEventReducer subtracting = new();
        RootReducer<SnapshotCacheGrainTestState> original = new([adding, subtracting]);
        RootReducer<SnapshotCacheGrainTestState> reordered = new([subtracting, adding]);
        SnapshotCacheGrainTestState initial = new();
        SnapshotCacheGrainTestState originalState = original.Reduce(initial, 5);
        SnapshotCacheGrainTestState reorderedState = reordered.Reduce(initial, 5);
        Assert.Equal(5, originalState.Value);
        Assert.Equal(-5, reorderedState.Value);
        Assert.Equal(0, initial.Value);
        Assert.NotSame(initial, originalState);
        Assert.NotSame(initial, reorderedState);
        Assert.NotEqual(original.GetReducerHash(), reordered.GetReducerHash());
        SnapshotStreamKey originalStream = new(
            "TEST.REDUCER.ORDER",
            "TEST.REDUCER.STATE.V1",
            "entity-1",
            original.GetReducerHash());
        SnapshotStreamKey reorderedStream = new(
            "TEST.REDUCER.ORDER",
            "TEST.REDUCER.STATE.V1",
            "entity-1",
            reordered.GetReducerHash());
        Assert.NotEqual(new(originalStream, 0), new SnapshotKey(reorderedStream, 0));
    }

    /// <summary>
    ///     Verifies that moving a different event's reducer preserves competing reducer priority and hash.
    /// </summary>
    [Fact]
    public void MovingIndependentReducerShouldKeepHashAndReplay()
    {
        AddingStateEventReducer adding = new();
        SubtractingStateEventReducer subtracting = new();
        TextStateEventReducer text = new();
        RootReducer<SnapshotCacheGrainTestState> original = new([adding, text, subtracting]);
        RootReducer<SnapshotCacheGrainTestState> reordered = new([text, adding, subtracting]);
        Assert.Equal(original.GetReducerHash(), reordered.GetReducerHash());
        Assert.Equal(original.Reduce(new(), 5), reordered.Reduce(new(), 5));
        Assert.Equal(original.Reduce(new(), "test"), reordered.Reduce(new(), "test"));
    }

    /// <summary>
    ///     Verifies that registering independent event types in the opposite order preserves the hash.
    /// </summary>
    [Fact]
    public void ReorderingIndependentEventTypesShouldKeepHashAndReplay()
    {
        AddingStateEventReducer adding = new();
        TextStateEventReducer text = new();
        RootReducer<SnapshotCacheGrainTestState> original = new([adding, text]);
        RootReducer<SnapshotCacheGrainTestState> reordered = new([text, adding]);
        Assert.Equal(original.GetReducerHash(), reordered.GetReducerHash());
        Assert.Equal(5, original.Reduce(new(), 5).Value);
        Assert.Equal(5, reordered.Reduce(new(), 5).Value);
        Assert.Equal(4, original.Reduce(new(), "test").Value);
        Assert.Equal(4, reordered.Reduce(new(), "test").Value);
    }

    /// <summary>
    ///     Verifies that equivalent registrations remain stable across root and reducer instances.
    /// </summary>
    [Fact]
    public void SameCompetingReducerOrderShouldKeepHashAndReplay()
    {
        RootReducer<SnapshotCacheGrainTestState> original = new(
            [new AddingStateEventReducer(), new SubtractingStateEventReducer()]);
        RootReducer<SnapshotCacheGrainTestState> equivalent = new(
            [new AddingStateEventReducer(), new SubtractingStateEventReducer()]);
        Assert.NotEmpty(original.GetReducerHash());
        Assert.Equal(original.GetReducerHash(), equivalent.GetReducerHash());
        Assert.Equal(original.Reduce(new(), 5), equivalent.Reduce(new(), 5));
    }

    /// <summary>
    ///     Verifies that stored snapshot reads agree with replay after a deployment's reducer priority changes.
    /// </summary>
    /// <param name="reversePriority">Whether the new deployment reverses the competing reducers.</param>
    /// <returns>The asynchronous test task.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SavedSnapshotShouldAgreeWithReplayForCurrentPriority(
        bool reversePriority
    )
    {
        AddingStateEventReducer adding = new();
        SubtractingStateEventReducer subtracting = new();
        RootReducer<SnapshotCacheGrainTestState> original = new([adding, subtracting]);
        RootReducer<SnapshotCacheGrainTestState> current = reversePriority
            ? new([subtracting, adding])
            : new([adding, subtracting]);
        SnapshotCacheGrainTestState savedState = original.Reduce(new(), 5);
        SnapshotCacheGrainTestState replayedState = current.Reduce(new(), 5);
        Assert.Equal(5, savedState.Value);
        Assert.Equal(reversePriority ? -5 : 5, replayedState.Value);
        SnapshotKey savedKey = new(
            new("TEST.REDUCER.ORDER", "TEST.REDUCER.STATE.V1", "entity-1", original.GetReducerHash()),
            0);
        SnapshotKey currentKey = new(
            new("TEST.REDUCER.ORDER", "TEST.REDUCER.STATE.V1", "entity-1", current.GetReducerHash()),
            0);
        SnapshotEnvelope savedEnvelope = new()
        {
            Data = [53],
            DataContentType = "application/json",
            DataSizeBytes = 1,
            ReducerHash = original.GetReducerHash(),
        };
        Mock<ISnapshotStorageReader> storage = new();
        storage.Setup(reader => reader.ReadAsync(It.IsAny<SnapshotKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SnapshotKey key, CancellationToken _) => key == savedKey ? savedEnvelope : null);
        Mock<ISnapshotStateConverter<SnapshotCacheGrainTestState>> converter = new();
        converter.Setup(value => value.FromEnvelope(savedEnvelope)).Returns(savedState);
        converter.Setup(value => value.ToEnvelope(It.IsAny<SnapshotCacheGrainTestState>(), current.GetReducerHash()))
            .Returns(
                new SnapshotEnvelope
                {
                    ReducerHash = current.GetReducerHash(),
                });
        BrookEvent brookEvent = new()
        {
            Data = [53],
            DataContentType = "application/json",
            DataSizeBytes = 1,
            EventType = "TEST.REDUCER.EVENT.V1",
            Id = "event-0",
            Source = "TEST.REDUCER.ORDER",
        };
        Mock<IBrookEventConverter> eventConverter = new();
        eventConverter.Setup(value => value.ToDomainEvent(brookEvent)).Returns(5);
        Mock<IBrookAsyncReaderGrain> reader = new();
        reader.Setup(value => value.ReadEventsAsync(
                new BrookPosition(0),
                new BrookPosition(0),
                It.IsAny<CancellationToken>()))
            .Returns(() => ReadEventAsync(brookEvent));
        Mock<IBrookGrainFactory> brooks = new();
        brooks.Setup(value => value.GetBrookAsyncReaderGrain(new("TEST.REDUCER.ORDER", "entity-1")))
            .Returns(reader.Object);
        Mock<ISnapshotPersisterGrain> persister = new();
        Mock<ISnapshotGrainFactory> snapshots = new();
        snapshots.Setup(value => value.GetSnapshotPersisterGrain(currentKey)).Returns(persister.Object);
        Mock<IGrainContext> context = new();
        context.Setup(value => value.GrainId).Returns(GrainId.Create("test", currentKey));
        SnapshotCacheGrain<SnapshotCacheGrainTestState> cache = new(
            context.Object,
            storage.Object,
            brooks.Object,
            current,
            converter.Object,
            snapshots.Object,
            Options.Create(new SnapshotRetentionOptions()),
            eventConverter.Object,
            NullLogger<SnapshotCacheGrain<SnapshotCacheGrainTestState>>.Instance);
        await cache.OnActivateAsync(TestContext.Current.CancellationToken);
        SnapshotCacheGrainTestState actual = await cache.GetStateAsync(TestContext.Current.CancellationToken);
        Assert.Equal(replayedState, actual);
        storage.Verify(value => value.ReadAsync(currentKey, It.IsAny<CancellationToken>()), Times.Once);
        converter.Verify(value => value.FromEnvelope(savedEnvelope), reversePriority ? Times.Never() : Times.Once());
        reader.Verify(
            value => value.ReadEventsAsync(new BrookPosition(0), new BrookPosition(0), It.IsAny<CancellationToken>()),
            reversePriority ? Times.Once() : Times.Never());
    }
}