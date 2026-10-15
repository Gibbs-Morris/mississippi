using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Brooks.Abstractions;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.State;
using Mississippi.DomainModeling.Runtime.Projections.ReferenceSetPages;

using MississippiTests.DomainModeling.Runtime.L0Tests.Projections.CatalogItem;
using MississippiTests.DomainModeling.Runtime.L0Tests.Projections.SavedItemsMembership;

using Moq;

using Orleans;


namespace MississippiTests.DomainModeling.Runtime.L0Tests.Projections.ReferenceSetPages;

/// <summary>Verifies failure, cancellation and diagnostic contracts at the new runtime boundaries.</summary>
public sealed class ReferenceSetPageBoundaryTests
{
    private static bool HasSelection(
        object state
    )
    {
        IReadOnlyList<KeyValuePair<string, object?>> fields = (IReadOnlyList<KeyValuePair<string, object?>>)state;
        return fields.Any(field => (field.Key == "ProjectionType") && Equals(field.Value, "NamedProjection")) &&
               fields.Any(field => (field.Key == "Offset") && Equals(field.Value, 2)) &&
               fields.Any(field => (field.Key == "Limit") && Equals(field.Value, 5)) &&
               fields.Any(field => (field.Key == "Version") && Equals(field.Value, 7L));
    }

    /// <summary>Rejects unavailable snapshots and independently validates calls across the grain boundary.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Fact]
    public async Task CacheRejectsMissingSnapshotsAndInvalidSelections()
    {
        UxProjectionVersionedCacheKey key = new("TEST.REFERENCESET.SAVEDITEMS", "collection", new(0));
        Mock<IUxProjectionVersionedCacheGrain<SavedItemsMembershipProjection>> whole = new(MockBehavior.Strict);
        whole.Setup(item => item.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((SavedItemsMembershipProjection?)null);
        Mock<IUxProjectionGrainFactory> projections = new(MockBehavior.Strict);
        projections.Setup(item => item.GetUxProjectionVersionedCacheGrain<SavedItemsMembershipProjection>(key))
            .Returns(whole.Object);
        ReferenceSetStateAccess<SavedItemsMembershipProjection, CatalogItemProjection> membership = new(
            state => state!.References,
            (state, references) => state! with
            {
                References = references,
            });
        ReferenceSetPageCacheGrain<SavedItemsMembershipProjection, CatalogItemProjection> cache = new(
            ReferenceSetPageFixture<SavedItemsMembershipProjection, CatalogItemProjection>.Context(key).Object,
            projections.Object,
            membership,
            NullLogger<ReferenceSetPageCacheGrain<SavedItemsMembershipProjection, CatalogItemProjection>>.Instance);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await cache.OnActivateAsync(TestContext.Current.CancellationToken));
        whole.Setup(item => item.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SavedItemsMembershipProjection());
        await cache.OnActivateAsync(TestContext.Current.CancellationToken);
        Assert.Empty((await cache.ReadAsync(int.MaxValue, 1, TestContext.Current.CancellationToken)).References);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await cache.ReadAsync(-1, 1, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await cache.ReadAsync(0, 0, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await cache.ReadAsync(0, 101, TestContext.Current.CancellationToken));
        using CancellationTokenSource canceled = new();
        await canceled.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await cache.ReadAsync(0, 1, canceled.Token));
        await cache.OnDeactivateAsync(default, TestContext.Current.CancellationToken);
    }

    /// <summary>Checks enabled structured events and the disabled logging fast path.</summary>
    [Fact]
    public void DiagnosticsRetainNamedFieldsAndHonorDisabledLevels()
    {
        Mock<ILogger> logger = new();
        logger.Setup(item => item.IsEnabled(LogLevel.Debug)).Returns(true);
        logger.Object.PageReadStarted("NamedProjection", 2, 5, 7);
        logger.Verify(
            item => item.Log(
                LogLevel.Debug,
                It.Is<EventId>(id => (id.Id == 1) && (id.Name == "PageReadStarted")),
                It.Is<It.IsAnyType>((state, _) => HasSelection(state)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        logger.Object.PageReadCompleted(7, 2, 1.5);
        logger.Object.PageIndexPrepared(7, 3, 2.5);
        logger.Object.PageCacheDeactivated(7);
        Assert.Equal(4, logger.Invocations.Count(item => item.Method.Name == "Log"));
        logger.Invocations.Clear();
        logger.Setup(item => item.IsEnabled(LogLevel.Debug)).Returns(false);
        logger.Object.PageReadStarted("Disabled", 0, 50, null);
        Assert.DoesNotContain(logger.Invocations, item => item.Method.Name == "Log");
    }

    /// <summary>Rejects future empty-history versions and observes cancellation after an uncancelable cursor call.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Fact]
    public async Task ReaderRejectsFutureEmptyHistoryAndCancellationDuringCursorRead()
    {
        Mock<IUxProjectionCursorGrain> cursor = new(MockBehavior.Strict);
        cursor.Setup(item => item.GetPositionAsync()).ReturnsAsync(new BrookPosition(-1));
        Mock<IUxProjectionGrainFactory> projections = new(MockBehavior.Strict);
        projections.Setup(item => item.GetUxProjectionCursorGrain<SavedItemsMembershipProjection>("collection"))
            .Returns(cursor.Object);
        Mock<IGrainFactory> grains = new(MockBehavior.Strict);
        ReferenceSetPageReader<SavedItemsMembershipProjection, CatalogItemProjection> reader = new(
            projections.Object,
            grains.Object,
            NullLogger<ReferenceSetPageReader<SavedItemsMembershipProjection, CatalogItemProjection>>.Instance);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await reader.ReadAsync(
            "collection",
            version: new(0),
            cancellationToken: TestContext.Current.CancellationToken));
        using CancellationTokenSource canceled = new();
        cursor.Setup(item => item.GetPositionAsync())
            .Returns(async () =>
            {
                await canceled.CancelAsync();
                return new(-1);
            });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await reader.ReadAsync("collection", cancellationToken: canceled.Token));
        grains.VerifyNoOtherCalls();
    }
}