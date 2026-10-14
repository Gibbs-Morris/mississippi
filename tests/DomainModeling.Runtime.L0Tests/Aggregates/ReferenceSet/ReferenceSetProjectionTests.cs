using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.Brooks.Abstractions.Cursor;
using Mississippi.Brooks.Abstractions.Factory;
using Mississippi.Brooks.Abstractions.Writer;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Runtime;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Events;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Reducers;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Registrations;
using Mississippi.Tributary.Abstractions;

using MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.PinnedItems.State;
using MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.SavedItems.Commands;
using MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.SavedItems.State;
using MississippiTests.DomainModeling.Runtime.L0Tests.Projections.CatalogItem;
using MississippiTests.DomainModeling.Runtime.L0Tests.Projections.PinnedItemsMembership;
using MississippiTests.DomainModeling.Runtime.L0Tests.Projections.PositionalMembership;
using MississippiTests.DomainModeling.Runtime.L0Tests.Projections.SavedItemsMembership;

using Moq;

using Orleans;
using Orleans.Runtime;


namespace MississippiTests.DomainModeling.Runtime.L0Tests.Aggregates.ReferenceSet;

/// <summary>Verifies named read-only consumers through aggregate append and the existing UX projection grains.</summary>
public sealed class ReferenceSetProjectionTests
{
    /// <summary>Creates the external Orleans context seam for a specific grain identity.</summary>
    /// <param name="key">The grain primary key.</param>
    /// <returns>The configured context mock.</returns>
    private static Mock<IGrainContext> Context(
        string key
    )
    {
        Mock<IGrainContext> context = new();
        context.Setup(item => item.GrainId).Returns(GrainId.Create("test", key));
        return context;
    }

    /// <summary>Executes aggregate writes and versioned UX reads using actual registrations and codecs.</summary>
    /// <typeparam name="TAggregate">The named collection aggregate.</typeparam>
    /// <typeparam name="TProjection">Its independently named membership projection.</typeparam>
    /// <param name="createAggregate">Creates empty aggregate state for replay.</param>
    /// <param name="createProjection">Creates empty projection state for replay.</param>
    /// <param name="members">Reads projection membership for assertions.</param>
    /// <returns>The asynchronous verification.</returns>
    private static async Task VerifyProjectionAsync<TAggregate, TProjection>(
        Func<TAggregate> createAggregate,
        Func<TProjection> createProjection,
        Func<TProjection, IReadOnlyDictionary<string, long>> members
    )
        where TAggregate : class
        where TProjection : class
    {
        using ServiceProvider provider = ReferenceSetFixture.Provider();
        IBrookEventConverter converter = provider.GetRequiredService<IBrookEventConverter>();
        IRootReducer<TAggregate> aggregateReducer = provider.GetRequiredService<IRootReducer<TAggregate>>();
        IRootReducer<TProjection> projectionReducer = provider.GetRequiredService<IRootReducer<TProjection>>();
        ISnapshotStateConverter<TProjection> snapshots =
            provider.GetRequiredService<ISnapshotStateConverter<TProjection>>();
        BrookKey brook = BrookKey.ForType<TAggregate>("collection");
        Assert.Equal(brook, BrookKey.ForType<TProjection>("collection"));
        Assert.NotEqual(
            SnapshotStorageNameHelper.GetStorageName<TAggregate>(),
            SnapshotStorageNameHelper.GetStorageName<TProjection>());
        Assert.IsType<ReferenceAddedReducer<TProjection, CatalogItemProjection>>(
            provider.GetRequiredService<IEventReducer<ReferenceAdded, TProjection>>());
        Assert.IsType<ReferenceRemovedReducer<TProjection, CatalogItemProjection>>(
            provider.GetRequiredService<IEventReducer<ReferenceRemoved, TProjection>>());
        Assert.Equal(2, provider.GetServices<IEventReducer<TProjection>>().Count());
        Assert.Null(provider.GetService<IRootCommandHandler<TProjection>>());
        Assert.Null(provider.GetService<ICommandHandler<AddCatalogItem, TProjection>>());
        Assert.Null(provider.GetService<ICommandHandler<RemoveCatalogItem, TProjection>>());
        Assert.Empty(provider.GetServices<IEventEffect<TProjection>>());
        Mock<IGrainFactory> sources = new(MockBehavior.Strict);
        Mock<IBrookGrainFactory> brooks = new(MockBehavior.Strict);
        Mock<IBrookCursorGrain> cursor = new(MockBehavior.Strict);
        Mock<IBrookWriterGrain> writer = new(MockBehavior.Strict);
        Mock<ISnapshotGrainFactory> aggregateSnapshots = new(MockBehavior.Strict);
        Mock<ISnapshotCacheGrain<TAggregate>> aggregateCache = new(MockBehavior.Strict);
        TAggregate? aggregateState = null;
        List<BrookEvent> history = [];
        cursor.Setup(item => item.GetLatestPositionAsync()).ReturnsAsync(() => new(history.Count - 1));
        brooks.Setup(item => item.GetBrookCursorGrain(brook)).Returns(cursor.Object);
        brooks.Setup(item => item.GetBrookWriterGrain(brook)).Returns(writer.Object);
        aggregateSnapshots.Setup(item => item.GetSnapshotCacheGrain<TAggregate>(It.IsAny<SnapshotKey>()))
            .Returns(aggregateCache.Object);
        aggregateCache.Setup(item => item.GetStateAsync(It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromResult(aggregateState!));
        writer.Setup(item => item.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImmutableArray<BrookEvent> appended, BrookPosition? expected, CancellationToken _) =>
            {
                Assert.Equal(history.Count == 0 ? null : new BrookPosition(history.Count - 1), expected);
                BrookEvent stored = Assert.Single(appended);
                Assert.Equal((string)brook, stored.Source);
                history.Add(stored);
                aggregateState = aggregateReducer.Reduce(
                    aggregateState ?? createAggregate(),
                    converter.ToDomainEvent(stored));
                return new(history.Count - 1);
            });
        GenericAggregateGrain<TAggregate> aggregate = new(
            Context("collection").Object,
            sources.Object,
            brooks.Object,
            converter,
            provider.GetRequiredService<IRootCommandHandler<TAggregate>>(),
            aggregateSnapshots.Object,
            aggregateReducer,
            Options.Create(new AggregateEffectOptions()),
            new FakeTimeProvider(),
            NullLogger<GenericAggregateGrain<TAggregate>>.Instance,
            [],
            Mock.Of<ISagaReminderRegistry>());
        await aggregate.OnActivateAsync(CancellationToken.None);
        Mock<IUxProjectionCursorGrain> projectionCursor = new(MockBehavior.Strict);
        projectionCursor.Setup(item => item.GetPositionAsync()).ReturnsAsync(() => new(history.Count - 1));
        Mock<IUxProjectionGrainFactory> projections = new(MockBehavior.Strict);
        projections.Setup(item => item.GetUxProjectionCursorGrain(new(brook.BrookName, brook.EntityId)))
            .Returns(projectionCursor.Object);
        Dictionary<long, IUxProjectionVersionedCacheGrain<TProjection>> versions = [];
        projections.Setup(item =>
                item.GetUxProjectionVersionedCacheGrain<TProjection>(It.IsAny<UxProjectionVersionedCacheKey>()))
            .Returns((UxProjectionVersionedCacheKey key) =>
            {
                Assert.Equal(brook.BrookName, key.BrookName);
                Assert.Equal(brook.EntityId, key.EntityId);
                return versions[key.Version.Value];
            });
        UxProjectionGrain<TProjection> projection = new(
            Context("collection").Object,
            projections.Object,
            NullLogger<UxProjectionGrain<TProjection>>.Instance);
        await projection.OnActivateAsync(CancellationToken.None);
        Assert.Equal(-1, (await projection.GetLatestVersionAsync()).Value);
        Assert.Null(await projection.GetAsync());
        Assert.Null(await projection.GetAtVersionAsync(new(-1)));
        projections.Verify(
            item => item.GetUxProjectionVersionedCacheGrain<TProjection>(It.IsAny<UxProjectionVersionedCacheKey>()),
            Times.Never);
        Assert.True((await aggregate.ExecuteAsync(new RemoveCatalogItem("absent"), CancellationToken.None)).Success);
        Assert.Empty(history);
        Assert.True((await aggregate.ExecuteAsync(new AddCatalogItem(" A ", 7), CancellationToken.None)).Success);
        CatalogItemProjection source = new()
        {
            Title = "before",
        };
        source = source with
        {
            Title = "after",
        };
        Assert.Equal("after", source.Title);
        Assert.True((await aggregate.ExecuteAsync(new AddCatalogItem(" A ", 99), CancellationToken.None)).Success);
        Assert.True((await aggregate.ExecuteAsync(new RemoveCatalogItem("absent"), CancellationToken.None)).Success);
        Assert.Single(history);
        Assert.True((await aggregate.ExecuteAsync(new AddCatalogItem("a", 0), CancellationToken.None)).Success);
        Assert.True((await aggregate.ExecuteAsync(new RemoveCatalogItem(" A "), CancellationToken.None)).Success);
        Assert.True((await aggregate.ExecuteAsync(new AddCatalogItem(" A ", 12), CancellationToken.None)).Success);
        Assert.Equal(4, history.Count);
        List<TProjection> retained = [createProjection()];
        for (int version = 0; version < history.Count; version++)
        {
            TProjection replay = projectionReducer.Reduce(retained[^1], converter.ToDomainEvent(history[version]));
            retained.Add(replay);
            SnapshotEnvelope envelope = snapshots.ToEnvelope(replay, projectionReducer.GetReducerHash());
            Assert.DoesNotContain("Title", Encoding.UTF8.GetString(envelope.Data.AsSpan()), StringComparison.Ordinal);
            Assert.DoesNotContain(
                "SourceType",
                Encoding.UTF8.GetString(envelope.Data.AsSpan()),
                StringComparison.Ordinal);
            TProjection restored = snapshots.FromEnvelope(envelope);
            Mock<ISnapshotCacheGrain<TProjection>> cache = new(MockBehavior.Strict);
            cache.Setup(item => item.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(restored);
            SnapshotKey expectedKey = new(
                new(
                    brook.BrookName,
                    SnapshotStorageNameHelper.GetStorageName<TProjection>(),
                    brook.EntityId,
                    projectionReducer.GetReducerHash()),
                version);
            Mock<ISnapshotGrainFactory> factory = new(MockBehavior.Strict);
            factory.Setup(item => item.GetSnapshotCacheGrain<TProjection>(expectedKey)).Returns(cache.Object);
            UxProjectionVersionedCacheKey key = new(brook.BrookName, brook.EntityId, new(version));
            UxProjectionVersionedCacheGrain<TProjection> versioned = new(
                Context(key).Object,
                factory.Object,
                projectionReducer,
                NullLogger<UxProjectionVersionedCacheGrain<TProjection>>.Instance);
            await versioned.OnActivateAsync(CancellationToken.None);
            versions.Add(version, versioned);
            factory.VerifyAll();
        }

        TProjection first = (await projection.GetAtVersionAsync(new(0)))!;
        Assert.Equal(7, members(first)[" A "]);
        Assert.Single(members(first));
        TProjection second = (await projection.GetAtVersionAsync(new(1)))!;
        Assert.Equal(7, members(second)[" A "]);
        Assert.Equal(0, members(second)["a"]);
        TProjection removed = (await projection.GetAtVersionAsync(new(2)))!;
        Assert.False(members(removed).ContainsKey(" A "));
        Assert.Single(members(removed));
        TProjection latest = (await projection.GetAsync())!;
        Assert.Equal(3, (await projection.GetLatestVersionAsync()).Value);
        Assert.Equal(12, members(latest)[" A "]);
        Assert.Equal(0, members(latest)["a"]);
        Assert.Same(first, await projection.GetAtVersionAsync(new(0)));
        Assert.Equal(7, members(first)[" A "]);
        Assert.Equal(4, history.Count);
        Assert.Empty(members(retained[0]));
        Assert.Single(members(retained[1]));
        Assert.Equal(7, members(retained[1])[" A "]);
        writer.Verify(
            item => item.AppendEventsAsync(
                It.IsAny<ImmutableArray<BrookEvent>>(),
                It.IsAny<BrookPosition?>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(4));
        sources.VerifyNoOtherCalls();
        brooks.VerifyAll();
        brooks.VerifyNoOtherCalls();
    }

    /// <summary>Independent projection registrations preserve their own immutable membership and typed metadata.</summary>
    [Fact]
    public void IndependentProjectionRegistrationsDoNotShareMembership()
    {
        using ServiceProvider provider = ReferenceSetFixture.Provider();
        SavedItemsMembershipProjection saved = provider
            .GetRequiredService<IRootReducer<SavedItemsMembershipProjection>>()
            .Reduce(
                new(),
                new ReferenceAdded
                {
                    SourceId = "saved-only",
                    ObservedSourceVersion = 7,
                });
        PinnedItemsMembershipProjection pinned = provider
            .GetRequiredService<IRootReducer<PinnedItemsMembershipProjection>>()
            .Reduce(
                new(),
                new ReferenceAdded
                {
                    SourceId = "pinned-only",
                    ObservedSourceVersion = 12,
                });
        Assert.Equal(7, saved.References.Members["saved-only"]);
        Assert.False(saved.References.Members.ContainsKey("pinned-only"));
        Assert.Equal(12, pinned.References.Members["pinned-only"]);
        Assert.False(pinned.References.Members.ContainsKey("saved-only"));
        Assert.Equal(typeof(CatalogItemProjection), saved.References.SourceType);
        Assert.Equal(typeof(CatalogItemProjection), pinned.References.SourceType);
    }

    /// <summary>A second named projection reads its own collection with separate typed DI and cache identities.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Fact]
    public Task PinnedMembershipUsesExactVersionUxReadsAsync() =>
        VerifyProjectionAsync(
            () => new PinnedItemsAggregate(),
            () => new PinnedItemsMembershipProjection(),
            state => state.References.Members);

    /// <summary>The feature helper alone supplies the UX factory used by documented named reads.</summary>
    [Fact]
    public void ProjectionOnlyRegistrationResolvesActualUxFactory()
    {
        ServiceCollection services = new();
        services.AddReferenceSetProjection<SavedItemsMembershipProjection, CatalogItemProjection>(
            state => state?.References ?? new(),
            (state, references) => (state ?? new()) with
            {
                References = references,
            });
        services.AddLogging();
        Mock<IGrainFactory> grains = new(MockBehavior.Strict);
        Mock<IUxProjectionGrain<SavedItemsMembershipProjection>> projection = new(MockBehavior.Strict);
        grains.Setup(item => item.GetGrain<IUxProjectionGrain<SavedItemsMembershipProjection>>("collection", null))
            .Returns(projection.Object);
        services.AddSingleton(grains.Object);
        using ServiceProvider provider = services.BuildServiceProvider();
        IUxProjectionGrainFactory factory = provider.GetRequiredService<IUxProjectionGrainFactory>();
        Assert.IsType<UxProjectionGrainFactory>(factory);
        Assert.Same(projection.Object, factory.GetUxProjectionGrain<SavedItemsMembershipProjection>("collection"));
        grains.VerifyAll();
    }

    /// <summary>The public helper rejects projection types the snapshot cache cannot construct.</summary>
    [Fact]
    public void ProjectionRegistrationRequiresPublicParameterlessConstructor()
    {
        MethodInfo registration =
            typeof(ReferenceSetRegistrations).GetMethod(nameof(ReferenceSetRegistrations.AddReferenceSetProjection))!;
        Assert.NotNull(
            registration.MakeGenericMethod(typeof(SavedItemsMembershipProjection), typeof(CatalogItemProjection)));
        Assert.Throws<ArgumentException>(() => registration.MakeGenericMethod(
            typeof(PositionalMembershipProjection),
            typeof(CatalogItemProjection)));
    }

    /// <summary>Projection registration rejects missing callbacks before installing services.</summary>
    [Fact]
    public void RegistrationRejectsNullArguments()
    {
        ServiceCollection services = new();
        Assert.Throws<ArgumentNullException>(() =>
            ReferenceSetRegistrations.AddReferenceSetProjection<SavedItemsMembershipProjection, CatalogItemProjection>(
                null!,
                state => state?.References ?? new(),
                (state, references) => new()
                {
                    References = references,
                }));
        Assert.Throws<ArgumentNullException>(() =>
            services.AddReferenceSetProjection<SavedItemsMembershipProjection, CatalogItemProjection>(
                null!,
                (state, references) => new()
                {
                    References = references,
                }));
        Assert.Throws<ArgumentNullException>(() =>
            services.AddReferenceSetProjection<SavedItemsMembershipProjection, CatalogItemProjection>(
                state => state?.References ?? new(),
                null!));
        Assert.Empty(services);
    }

    /// <summary>Saved membership preserves empty, no-op, ordinal and historical collection contracts.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Fact]
    public Task SavedMembershipUsesExactVersionUxReadsAsync() =>
        VerifyProjectionAsync(
            () => new SavedItemsAggregate(),
            () => new SavedItemsMembershipProjection(),
            state => state.References.Members);
}