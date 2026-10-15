using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Brooks.Abstractions;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Abstractions.Projections.ReferenceSetPages;
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
    /// <summary>Checks the masked collection identity in a structured event.</summary>
    /// <param name="state">The generated structured fields.</param>
    /// <param name="entityHash">The expected masked entity.</param>
    /// <returns>Whether the identity is present.</returns>
    private static bool HasIdentity(
        object state,
        string entityHash
    )
    {
        IReadOnlyList<KeyValuePair<string, object?>> fields = (IReadOnlyList<KeyValuePair<string, object?>>)state;
        return fields.Any(field => (field.Key == "EntityHash") && Equals(field.Value, entityHash));
    }

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
        int indexReads = 0;
        ReferenceSetStateAccess<SavedItemsMembershipProjection, CatalogItemProjection> membership = new(
            state =>
            {
                indexReads++;
                return state!.References;
            },
            (state, references) => state! with
            {
                References = references,
            });
        Mock<ILogger<ReferenceSetPageCacheGrain<SavedItemsMembershipProjection, CatalogItemProjection>>> logger = new();
        logger.Setup(item => item.IsEnabled(LogLevel.Debug)).Returns(true);
        ReferenceSetPageCacheGrain<SavedItemsMembershipProjection, CatalogItemProjection> cache = new(
            ReferenceSetPageFixture<SavedItemsMembershipProjection, CatalogItemProjection>.Context(key).Object,
            projections.Object,
            membership,
            logger.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await cache.OnActivateAsync(TestContext.Current.CancellationToken));
        whole.Setup(item => item.GetAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Incomplete requested range."));
        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await cache.OnActivateAsync(TestContext.Current.CancellationToken));
        Assert.Equal("Incomplete requested range.", failure.Message);
        Assert.Equal(0, indexReads);
        whole.Setup(item => item.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SavedItemsMembershipProjection());
        await cache.OnActivateAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, indexReads);
        logger.Verify(
            item => item.Log(
                LogLevel.Debug,
                It.Is<EventId>(id => (id.Id == 5) && (id.Name == "PageCacheActivating")),
                It.Is<It.IsAnyType>((state, _) => HasIdentity(state, ReferenceSetPageLoggerExtensions.GetEntityHash("collection"))),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
        logger.Verify(
            item => item.Log(
                LogLevel.Debug,
                It.Is<EventId>(id => id.Id == 1),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
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
        string entityHash = ReferenceSetPageLoggerExtensions.GetEntityHash("collection");
        Assert.Equal(64, entityHash.Length);
        Assert.Equal(entityHash, ReferenceSetPageLoggerExtensions.GetEntityHash("collection"));
        Assert.NotEqual(entityHash, ReferenceSetPageLoggerExtensions.GetEntityHash("other-collection"));
        Assert.DoesNotContain("collection", entityHash, StringComparison.OrdinalIgnoreCase);
        logger.Object.PageReadStarted(entityHash, "NamedProjection", 2, 5, 7);
        logger.Verify(
            item => item.Log(
                LogLevel.Debug,
                It.Is<EventId>(id => (id.Id == 1) && (id.Name == "PageReadStarted")),
                It.Is<It.IsAnyType>((state, _) => HasSelection(state) && HasIdentity(state, entityHash)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        logger.Object.PageReadCompleted(entityHash, 7, 2, 1.5);
        logger.Object.PageIndexPrepared(entityHash, 7, 3, 2.5);
        logger.Object.PageCacheDeactivated(entityHash, 7);
        logger.Object.PageCacheActivating(entityHash, "NamedProjection", 7);
        Assert.Equal(5, logger.Invocations.Count(item => item.Method.Name == "Log"));
        Assert.All(
            logger.Invocations.Where(item => item.Method.Name == "Log"),
            invocation => Assert.True(HasIdentity(invocation.Arguments[2], entityHash)));
        logger.Invocations.Clear();
        logger.Setup(item => item.IsEnabled(LogLevel.Debug)).Returns(false);
        logger.Object.PageReadStarted(entityHash, "Disabled", 0, 50, null);
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
        projections.Setup(item => item.GetUxProjectionCursorGrain(
                new UxProjectionCursorKey("TEST.REFERENCESET.SAVEDITEMS", "collection")))
            .Returns(cursor.Object);
        Mock<IGrainFactory> grains = new(MockBehavior.Strict);
        ReferenceSetPageReader<SavedItemsMembershipProjection, CatalogItemProjection> reader = new(
            projections.Object,
            grains.Object,
            NullLogger<ReferenceSetPageReader<SavedItemsMembershipProjection, CatalogItemProjection>>.Instance);
        grains.Setup(item => item.GetGrain<IReferenceSetPageCacheGrain<SavedItemsMembershipProjection, CatalogItemProjection>>(
                It.IsAny<string>(), null))
            .Throws(new InvalidOperationException("The requested exact snapshot is unavailable."));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await reader.ReadAsync(
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
        cursor.Verify(item => item.GetPositionAsync(), Times.Once);
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await reader.ReadAsync(
            null!,
            version: new(-1),
            cancellationToken: TestContext.Current.CancellationToken));
    }
}