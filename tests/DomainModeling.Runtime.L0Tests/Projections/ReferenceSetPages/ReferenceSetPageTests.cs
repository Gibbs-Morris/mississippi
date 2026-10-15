using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Serialization.Json;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Abstractions.Projections.ReferenceSetPages;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Events;
using Mississippi.DomainModeling.Runtime.Projections.ReferenceSetPages;

using MississippiTests.DomainModeling.Runtime.L0Tests.Projections.CatalogItem;
using MississippiTests.DomainModeling.Runtime.L0Tests.Projections.SavedItemsMembership;

using Moq;

using Orleans;
using Orleans.Serialization;


namespace MississippiTests.DomainModeling.Runtime.L0Tests.Projections.ReferenceSetPages;

/// <summary>Proves the compiled public page consumer and causal runtime boundaries.</summary>
public sealed class ReferenceSetPageTests
{
    private static readonly string[] FirstIds = [" a ", "A"];

    private static readonly string[] NextIds = ["a", "z"];

    private static ServiceProvider Provider(
        Mock<IGrainFactory> grains
    )
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(grains.Object);
        services.AddReferenceSetProjectionPages<SavedItemsMembershipProjection, CatalogItemProjection>(
            state => state?.References ?? new(),
            (state, references) => (state ?? new()) with
            {
                References = references,
            });
        services.AddReferenceSetProjectionPages<FollowedListsMembershipProjection, SavedItemsMembershipProjection>(
            state => state?.References ?? new(),
            (state, references) => (state ?? new()) with
            {
                References = references,
            });
        services.AddJsonSerialization();
        services.AddSerializer();
        return services.BuildServiceProvider();
    }

    /// <summary>Pairs one captured cursor with immutable data and preserves pinned traversal through writes.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Fact]
    public async Task CaptureAndPinnedTraversalRemainCausalAcrossWrites()
    {
        Mock<IGrainFactory> grains = new(MockBehavior.Strict);
        using ServiceProvider provider = Provider(grains);
        ReferenceSetPageFixture<SavedItemsMembershipProjection, CatalogItemProjection> fixture = new(provider, grains);
        await fixture.AppendAsync(
            new ReferenceAdded
            {
                SourceId = "a",
                ObservedSourceVersion = 7,
            });
        await fixture.AppendAsync(
            new ReferenceAdded
            {
                SourceId = "b",
                ObservedSourceVersion = 8,
            });
        fixture.BeforePageResolution = () => fixture.Append(
            new ReferenceAdded
            {
                SourceId = "c",
                ObservedSourceVersion = 20,
            });
        ReferenceSetPage<SavedItemsMembershipProjection, CatalogItemProjection> captured =
            await fixture.Reader.ReadAsync(
                "collection",
                limit: 1,
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, fixture.CursorReads);
        Assert.Equal(3, fixture.Appends);
        Assert.Equal(2, fixture.Latest);
        Assert.Equal(1, captured.Version.Value);
        Assert.Equal(2, captured.TotalCount);
        Assert.Equal("a", Assert.Single(captured.References).SourceId);
        fixture.BeforePageResolution = null;
        await fixture.AppendAsync(
            new ReferenceRemoved
            {
                SourceId = "a",
            });
        await fixture.AppendAsync(
            new ReferenceAdded
            {
                SourceId = "a",
                ObservedSourceVersion = 99,
            });
        fixture.Latest = -1;
        ReferenceSetPage<SavedItemsMembershipProjection, CatalogItemProjection> pinned = await fixture.Reader.ReadAsync(
            "collection",
            1,
            1,
            captured.Version,
            TestContext.Current.CancellationToken);
        Assert.Equal(1, fixture.CursorReads);
        Assert.Equal(-1, fixture.Latest);
        Assert.Equal(captured.Version, pinned.Version);
        Assert.Equal(2, pinned.TotalCount);
        Assert.Equal("b", Assert.Single(pinned.References).SourceId);
        ReferenceSetPage<SavedItemsMembershipProjection, CatalogItemProjection> historical =
            await fixture.Reader.ReadAsync(
                "collection",
                limit: 1,
                version: captured.Version,
                cancellationToken: TestContext.Current.CancellationToken);
        fixture.Latest = fixture.Appends - 1;
        ReferenceSetPage<SavedItemsMembershipProjection, CatalogItemProjection> current =
            await fixture.Reader.ReadAsync(
                "collection",
                limit: 1,
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(7, historical.References[0].ObservedSourceVersion);
        Assert.Equal(99, current.References[0].ObservedSourceVersion);
        Assert.Throws<ArgumentOutOfRangeException>(() => new BrookPosition(-2));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await fixture.Reader.ReadAsync(
            "collection",
            version: new(5),
            cancellationToken: TestContext.Current.CancellationToken));
    }

    /// <summary>Preserves requested empty selections, including overflow boundaries and genuine empty history.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Fact]
    public async Task EmptyHistoryAndExtremeOffsetsRetainSelection()
    {
        Mock<IGrainFactory> grains = new(MockBehavior.Strict);
        using ServiceProvider provider = Provider(grains);
        ReferenceSetPageFixture<SavedItemsMembershipProjection, CatalogItemProjection> fixture = new(provider, grains);
        ReferenceSetPage<SavedItemsMembershipProjection, CatalogItemProjection> empty = await fixture.Reader.ReadAsync(
            "collection",
            int.MaxValue,
            100,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(-1, empty.Version.Value);
        Assert.Equal(int.MaxValue, empty.Offset);
        Assert.Equal(100, empty.Limit);
        Assert.Equal(0, empty.TotalCount);
        Assert.Empty(empty.References);
        IUxProjectionGrain<SavedItemsMembershipProjection> ordinary = await fixture.OrdinaryAsync();
        Assert.Null(await ordinary.GetAsync(TestContext.Current.CancellationToken));
        Assert.Null(await ordinary.GetAtVersionAsync(new(-1), TestContext.Current.CancellationToken));
        await fixture.AppendAsync(
            new ReferenceAdded
            {
                SourceId = "a",
                ObservedSourceVersion = long.MaxValue,
            });
        foreach (int offset in new[] { 1, 2, int.MaxValue })
        {
            ReferenceSetPage<SavedItemsMembershipProjection, CatalogItemProjection> beyond =
                await fixture.Reader.ReadAsync(
                    "collection",
                    offset,
                    100,
                    cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(offset, beyond.Offset);
            Assert.Equal(100, beyond.Limit);
            Assert.Equal(1, beyond.TotalCount);
            Assert.Equal(0, beyond.Version.Value);
            Assert.Empty(beyond.References);
        }

        Assert.Single((await ordinary.GetAsync(TestContext.Current.CancellationToken))!.References.Members);
        ReferenceSetPage<SavedItemsMembershipProjection, CatalogItemProjection> observed =
            await fixture.Reader.ReadAsync(
                "collection",
                limit: 1,
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(long.MaxValue, observed.References[0].ObservedSourceVersion);
        Assert.Empty(
            (await fixture.Reader.ReadAsync(
                "collection",
                version: new(-1),
                cancellationToken: TestContext.Current.CancellationToken)).References);
        await fixture.AppendAsync(
            new ReferenceRemoved
            {
                SourceId = "a",
            });
        ReferenceSetPage<SavedItemsMembershipProjection, CatalogItemProjection> removed =
            await fixture.Reader.ReadAsync("collection", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, removed.Version.Value);
        Assert.Equal(0, removed.TotalCount);
        Assert.Empty(removed.References);
    }

    /// <summary>Resolves two independent projection/source pairs in the same application provider.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Fact]
    public async Task IndependentConsumersKeepTypedIdentityAndState()
    {
        Mock<IGrainFactory> grains = new(MockBehavior.Strict);
        using ServiceProvider provider = Provider(grains);
        ReferenceSetPageFixture<SavedItemsMembershipProjection, CatalogItemProjection> saved = new(
            provider,
            grains,
            "saved");
        ReferenceSetPageFixture<FollowedListsMembershipProjection, SavedItemsMembershipProjection> followed = new(
            provider,
            grains,
            "followed");
        await saved.AppendAsync(
            new ReferenceAdded
            {
                SourceId = "catalog-item",
                ObservedSourceVersion = 5,
            });
        await followed.AppendAsync(
            new ReferenceAdded
            {
                SourceId = "saved-list",
                ObservedSourceVersion = 10,
            });
        ReferenceSetPage<SavedItemsMembershipProjection, CatalogItemProjection> savedPage =
            await saved.Reader.ReadAsync("saved", cancellationToken: TestContext.Current.CancellationToken);
        ReferenceSetPage<FollowedListsMembershipProjection, SavedItemsMembershipProjection> followedPage =
            await followed.Reader.ReadAsync("followed", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("catalog-item", Assert.Single(savedPage.References).SourceId);
        Assert.Equal("saved-list", Assert.Single(followedPage.References).SourceId);
        Assert.Null(provider.GetService<IRootCommandHandler<FollowedListsMembershipProjection>>());
        Assert.Empty(provider.GetServices<IEventEffect<FollowedListsMembershipProjection>>());
        Assert.NotEqual(saved.Reader.GetType(), followed.Reader.GetType());
    }

    /// <summary>Rejects invalid selections before resolving any cursor or cache.</summary>
    /// <param name="offset">The invalid offset.</param>
    /// <param name="limit">The invalid limit.</param>
    /// <returns>The asynchronous verification.</returns>
    [Theory]
    [InlineData(-1, 50)]
    [InlineData(0, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 101)]
    [InlineData(int.MinValue, int.MaxValue)]
    public async Task InvalidSelectionHasNoRuntimeCalls(
        int offset,
        int limit
    )
    {
        Mock<IGrainFactory> grains = new(MockBehavior.Strict);
        using ServiceProvider provider = Provider(grains);
        IReferenceSetPageReader<SavedItemsMembershipProjection, CatalogItemProjection> reader =
            provider
                .GetRequiredService<IReferenceSetPageReader<SavedItemsMembershipProjection, CatalogItemProjection>>();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await reader.ReadAsync(
            "collection",
            offset,
            limit,
            cancellationToken: TestContext.Current.CancellationToken));
        grains.VerifyNoOtherCalls();
    }

    /// <summary>Proves default/bounded selection, ordinal traversal, source floors and transport serialization.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Fact]
    public async Task NamedConsumerReturnsBoundedOrdinalPagesAndDefaults()
    {
        Mock<IGrainFactory> grains = new(MockBehavior.Strict);
        using ServiceProvider provider = Provider(grains);
        ReferenceSetPageFixture<SavedItemsMembershipProjection, CatalogItemProjection> fixture = new(provider, grains);
        foreach (string id in new[] { "z", "a", "A", " a " })
        {
            await fixture.AppendAsync(
                new ReferenceAdded
                {
                    SourceId = id,
                    ObservedSourceVersion = id.Length,
                });
        }

        ReferenceSetPage<SavedItemsMembershipProjection, CatalogItemProjection> first = await fixture.Reader.ReadAsync(
            "collection",
            limit: 2,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(0, first.Offset);
        Assert.Equal(2, first.Limit);
        Assert.Equal(4, first.TotalCount);
        Assert.Equal(3, first.Version.Value);
        Assert.Equal(FirstIds, first.References.Select(item => item.SourceId));
        Assert.Equal(new long[] { 3, 1 }, first.References.Select(item => item.ObservedSourceVersion));
        ReferenceSetPage<SavedItemsMembershipProjection, CatalogItemProjection> defaults =
            await fixture.Reader.ReadAsync("collection", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(50, defaults.Limit);
        Assert.Equal(4, defaults.References.Length);
        ReferenceSetPage<SavedItemsMembershipProjection, CatalogItemProjection> next = await fixture.Reader.ReadAsync(
            "collection",
            2,
            100,
            first.Version,
            TestContext.Current.CancellationToken);
        Assert.Equal(NextIds, next.References.Select(item => item.SourceId));
        ReferenceSetPage<SavedItemsMembershipProjection, CatalogItemProjection>? roundTrip =
            provider.GetRequiredService<Serializer>()
                .Deserialize<ReferenceSetPage<SavedItemsMembershipProjection, CatalogItemProjection>>(
                    provider.GetRequiredService<Serializer>().SerializeToArray(first));
        Assert.NotNull(roundTrip);
        Assert.Equal(typeof(SavedItemsMembershipProjection), roundTrip.ProjectionType);
        Assert.Equal(typeof(CatalogItemProjection), roundTrip.SourceType);
        string json = JsonSerializer.Serialize(first);
        Assert.DoesNotContain("ProjectionType", json, StringComparison.Ordinal);
        Assert.DoesNotContain("SourceType", json, StringComparison.Ordinal);
        Assert.Equal(first.References.ToArray(), roundTrip.References.ToArray());
        Assert.Equal(first.Version, roundTrip.Version);
        Assert.Equal(first.Offset, roundTrip.Offset);
        Assert.Equal(first.Limit, roundTrip.Limit);
        Assert.Equal(first.TotalCount, roundTrip.TotalCount);
    }

    /// <summary>Reuses one activation index across more than 32 distinct selections without writes or source calls.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Fact]
    public async Task RepeatedPagesReuseIndexAndPreserveOrdinaryReads()
    {
        Mock<IGrainFactory> grains = new(MockBehavior.Strict);
        using ServiceProvider provider = Provider(grains);
        ReferenceSetPageFixture<SavedItemsMembershipProjection, CatalogItemProjection> fixture = new(provider, grains);
        await fixture.AppendAsync(
            new ReferenceAdded
            {
                SourceId = "a",
                ObservedSourceVersion = 3,
            });
        int preparations = fixture.IndexPreparations;
        Assert.Equal(1, preparations);
        int appends = fixture.Appends;
        for (int offset = 0; offset < 65; offset++)
        {
            ReferenceSetPage<SavedItemsMembershipProjection, CatalogItemProjection> page =
                await fixture.Reader.ReadAsync(
                    "collection",
                    offset,
                    1,
                    cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(offset, page.Offset);
            Assert.Equal(1, page.TotalCount);
            Assert.Equal(offset == 0 ? 1 : 0, page.References.Length);
        }

        Assert.Equal(preparations, fixture.IndexPreparations);
        Assert.Equal(appends, fixture.Appends);
        IUxProjectionGrain<SavedItemsMembershipProjection> ordinary = await fixture.OrdinaryAsync();
        Assert.Equal(
            3,
            (await ordinary.GetAtVersionAsync(new(0), TestContext.Current.CancellationToken))!.References.Members["a"]);
        using CancellationTokenSource canceled = new();
        await canceled.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await fixture.Reader.ReadAsync("collection", cancellationToken: canceled.Token));
    }
}