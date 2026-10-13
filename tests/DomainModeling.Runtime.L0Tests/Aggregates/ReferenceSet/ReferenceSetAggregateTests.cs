using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Abstractions.Cursor;
using Mississippi.Brooks.Abstractions.Factory;
using Mississippi.Brooks.Abstractions.Writer;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.State;
using Mississippi.DomainModeling.Runtime;
using Mississippi.Tributary.Abstractions;

using MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.PinnedItems.State;
using MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.SavedItems.Commands;
using MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.SavedItems.State;
using MississippiTests.DomainModeling.Runtime.L0Tests.Projections.CatalogItem;

using Moq;

using Orleans;
using Orleans.Runtime;


namespace MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.ReferenceSet;

/// <summary>Exercises actual aggregate dispatch and conversion with only the external storage grain seams mocked.</summary>
public sealed class ReferenceSetAggregateTests
{
    private static async Task VerifyAggregateAsync<TAggregate>(
        Func<TAggregate> create,
        Func<TAggregate, ReferenceSetState<CatalogItemProjection>> read
    )
        where TAggregate : class
    {
        using ServiceProvider provider = ReferenceSetFixture.Provider();
        IBrookEventConverter converter = provider.GetRequiredService<IBrookEventConverter>();
        IRootReducer<TAggregate> reducer = provider.GetRequiredService<IRootReducer<TAggregate>>();
        Mock<IGrainContext> context = new();
        context.Setup(item => item.GrainId).Returns(GrainId.Create("test", "collection"));
        Mock<IGrainFactory> sourceGrains = new(MockBehavior.Strict);
        Mock<IBrookGrainFactory> brooks = new(MockBehavior.Strict);
        Mock<IBrookCursorGrain> cursor = new(MockBehavior.Strict);
        Mock<IBrookWriterGrain> writer = new(MockBehavior.Strict);
        Mock<ISnapshotGrainFactory> snapshots = new(MockBehavior.Strict);
        Mock<ISnapshotCacheGrain<TAggregate>> cache = new(MockBehavior.Strict);
        TAggregate? state = null;
        long position = -1;
        List<BrookEvent> persisted = [];
        cursor.Setup(item => item.GetLatestPositionAsync()).ReturnsAsync(() => new(position));
        brooks.Setup(item => item.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursor.Object);
        brooks.Setup(item => item.GetBrookWriterGrain(It.IsAny<BrookKey>())).Returns(writer.Object);
        snapshots.Setup(item => item.GetSnapshotCacheGrain<TAggregate>(It.IsAny<SnapshotKey>())).Returns(cache.Object);
        cache.Setup(item => item.GetStateAsync(It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromResult(state!));
        writer.Setup(item => item.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImmutableArray<BrookEvent> appended, BrookPosition? expected, CancellationToken _) =>
            {
                if (position == -1)
                {
                    Assert.Null(expected);
                }
                else
                {
                    Assert.Equal(position, expected!.Value.Value);
                }

                Assert.Single(appended);
                foreach (BrookEvent stored in appended)
                {
                    Assert.Equal((string)BrookKey.ForType<TAggregate>("collection"), stored.Source);
                    state = reducer.Reduce(state ?? create(), converter.ToDomainEvent(stored));
                    persisted.Add(stored);
                    position++;
                }

                return new(position);
            });
        GenericAggregateGrain<TAggregate> grain = new(
            context.Object,
            sourceGrains.Object,
            brooks.Object,
            converter,
            provider.GetRequiredService<IRootCommandHandler<TAggregate>>(),
            snapshots.Object,
            reducer,
            Options.Create(new AggregateEffectOptions()),
            TimeProvider.System,
            NullLogger<GenericAggregateGrain<TAggregate>>.Instance,
            [],
            Mock.Of<ISagaReminderRegistry>());
        await grain.OnActivateAsync(CancellationToken.None);
        Assert.Null(await grain.GetStateAsync(CancellationToken.None));
        Assert.True(
            (await grain.ExecuteAsync(new RemoveCatalogItem("absent"), new BrookPosition(-1), CancellationToken.None))
            .Success);
        Assert.Equal(-1, position);
        Assert.Null(await grain.GetStateAsync(CancellationToken.None));
        snapshots.Verify(item => item.GetSnapshotCacheGrain<TAggregate>(It.IsAny<SnapshotKey>()), Times.Never);
        writer.Verify(
            item => item.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        AddCatalogItem add = new("A", 7);
        Assert.True((await grain.ExecuteAsync(add, new BrookPosition(-1), CancellationToken.None)).Success);
        Assert.Equal(0, position);
        TAggregate first = (await grain.GetStateAsync(CancellationToken.None))!;
        Assert.Equal(7, read(first).Members["A"]);
        Assert.True(
            (await grain.ExecuteAsync(
                add with
                {
                    ObservedSourceVersion = 99,
                },
                new BrookPosition(0),
                CancellationToken.None)).Success);
        OperationResult conflict = await grain.ExecuteAsync(add, new BrookPosition(-1), CancellationToken.None);
        Assert.False(conflict.Success);
        Assert.Equal(AggregateErrorCodes.ConcurrencyConflict, conflict.ErrorCode);
        OperationResult malformed = await grain.ExecuteAsync(
            add with
            {
                ObservedSourceVersion = -1,
            },
            CancellationToken.None);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, malformed.ErrorCode);
        CatalogItemProjection source = new()
        {
            Title = "before",
        };
        source = source with
        {
            Title = "after",
        };
        Assert.Equal("after", source.Title);
        Assert.Same(first, await grain.GetStateAsync(CancellationToken.None));
        Assert.Single(persisted);
        Assert.True((await grain.ExecuteAsync(new RemoveCatalogItem("absent"), CancellationToken.None)).Success);
        Assert.Single(persisted);
        Assert.True(
            (await grain.ExecuteAsync(
                new RemoveCatalogItem("A"),
                new BrookPosition(0),
                CancellationToken.None)).Success);
        Assert.True(
            (await grain.ExecuteAsync(
                add with
                {
                    ObservedSourceVersion = 12,
                },
                new BrookPosition(1),
                CancellationToken.None)).Success);
        Assert.Equal(2, position);
        Assert.Equal(3, persisted.Count);
        Assert.Equal(12, read((await grain.GetStateAsync(CancellationToken.None))!).Members["A"]);
        Assert.Equal(7, read(first).Members["A"]);
        writer.Verify(
            item => item.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));
        sourceGrains.VerifyNoOtherCalls();
    }

    /// <summary>A second named collection reuses the same behavior through the actual aggregate path.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Fact]
    public Task PinnedCollectionUsesNormalAggregatePathAsync() =>
        VerifyAggregateAsync(() => new PinnedItemsAggregate(), state => state.References);

    /// <summary>A saved collection uses normal append, version conflicts and untouched-stream handling.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Fact]
    public Task SavedCollectionUsesNormalAggregatePathAsync() =>
        VerifyAggregateAsync(() => new SavedItemsAggregate(), state => state.References);
}