using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Abstractions.Projections.ReferenceSetPages;
using Mississippi.DomainModeling.Runtime;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.State;
using Mississippi.DomainModeling.Runtime.Projections.ReferenceSetPages;
using Mississippi.Tributary.Abstractions;

using Moq;

using Orleans;
using Orleans.Runtime;


namespace MississippiTests.DomainModeling.Runtime.L0Tests.Projections.ReferenceSetPages;

/// <summary>Runs real reader, replay, serialization, whole-cache and page-cache behavior over external seams.</summary>
/// <typeparam name="TProjection">The named membership projection.</typeparam>
/// <typeparam name="TSource">Its source projection type.</typeparam>
internal sealed class ReferenceSetPageFixture<TProjection, TSource>
    where TProjection : class, new()
{
    private readonly List<object> history = [];

    private readonly Dictionary<long, ReferenceSetPageCacheGrain<TProjection, TSource>> pageCaches = [];

    private readonly Dictionary<long, IUxProjectionVersionedCacheGrain<TProjection>> wholeCaches = [];

    /// <summary>Initializes a new instance of the <see cref="ReferenceSetPageFixture{TProjection, TSource}" /> class.</summary>
    /// <param name="provider">The shared application registrations.</param>
    /// <param name="grains">The strict external Orleans proxy seam.</param>
    /// <param name="entityId">The independently keyed collection entity.</param>
    public ReferenceSetPageFixture(
        ServiceProvider provider,
        Mock<IGrainFactory> grains,
        string entityId = "collection"
    )
    {
        Provider = provider;
        EntityId = entityId;
        Reader = provider.GetRequiredService<IReferenceSetPageReader<TProjection, TSource>>();
        Mock<IUxProjectionCursorGrain> cursor = new(MockBehavior.Strict);
        cursor.Setup(item => item.GetPositionAsync())
            .ReturnsAsync(() =>
            {
                CursorReads++;
                return new(Latest);
            });
        UxProjectionCursorKey cursorKey = new(BrookNameHelper.GetBrookName<TProjection>(), entityId);
        grains.Setup(item => item.GetGrain<IUxProjectionCursorGrain>(cursorKey, null)).Returns(cursor.Object);
        grains.Setup(item => item.GetGrain<IUxProjectionVersionedCacheGrain<TProjection>>(
                It.Is<string>(text => text.StartsWith((string)cursorKey + "|", StringComparison.Ordinal)),
                null))
            .Returns((string text, string? _) =>
            {
                UxProjectionVersionedCacheKey key = ValidateKey(text);
                return wholeCaches[key.Version.Value];
            });
        grains.Setup(item => item.GetGrain<IReferenceSetPageCacheGrain<TProjection, TSource>>(
                It.Is<string>(text => text.StartsWith((string)cursorKey + "|", StringComparison.Ordinal)),
                null))
            .Returns((string text, string? _) =>
            {
                UxProjectionVersionedCacheKey key = ValidateKey(text);
                BeforePageResolution?.Invoke();
                return pageCaches.TryGetValue(
                    key.Version.Value,
                    out ReferenceSetPageCacheGrain<TProjection, TSource>? page)
                    ? page
                    : throw new InvalidOperationException("The requested exact snapshot is unavailable.");
            });
    }

    /// <summary>Gets the membership append counter.</summary>
    public int Appends => history.Count;

    /// <summary>Gets or sets a controlled write between cursor capture and page resolution.</summary>
    public Action? BeforePageResolution { get; set; }

    /// <summary>Gets the number of cursor reads.</summary>
    public int CursorReads { get; private set; }

    /// <summary>Gets the number of index input enumerations.</summary>
    public int IndexPreparations { get; private set; }

    /// <summary>Gets or sets the controlled cached cursor.</summary>
    public long Latest { get; set; } = -1;

    /// <summary>Gets the application-facing actual reader resolved from shared DI.</summary>
    public IReferenceSetPageReader<TProjection, TSource> Reader { get; }

    private string EntityId { get; }

    private ServiceProvider Provider { get; }

    /// <summary>Creates the external Orleans activation context.</summary>
    /// <param name="key">The grain key.</param>
    /// <returns>The strict context seam.</returns>
    public static Mock<IGrainContext> Context(
        string key
    )
    {
        Mock<IGrainContext> context = new(MockBehavior.Strict);
        context.Setup(item => item.GrainId).Returns(GrainId.Create("test", key));
        return context;
    }

    /// <summary>Appends a fact while leaving already activated versions immutable.</summary>
    /// <param name="fact">The membership event.</param>
    public void Append(
        object fact
    )
    {
        history.Add(fact);
        Latest = history.Count - 1;
    }

    /// <summary>Appends a membership fact and prepares its real runtime caches.</summary>
    /// <param name="fact">The ordinary membership event.</param>
    /// <returns>The asynchronous cache activation.</returns>
    public async Task AppendAsync(
        object fact
    )
    {
        Append(fact);
        IRootReducer<TProjection> reducer = Provider.GetRequiredService<IRootReducer<TProjection>>();
        TProjection state = new();
        foreach (object item in history)
        {
            state = reducer.Reduce(state, item);
        }

        ISnapshotStateConverter<TProjection> converter =
            Provider.GetRequiredService<ISnapshotStateConverter<TProjection>>();
        TProjection restored = converter.FromEnvelope(converter.ToEnvelope(state, reducer.GetReducerHash()));
        UxProjectionVersionedCacheKey key = new(BrookNameHelper.GetBrookName<TProjection>(), EntityId, new(Latest));
        SnapshotKey expected = new(
            new(
                key.BrookName,
                SnapshotStorageNameHelper.GetStorageName<TProjection>(),
                EntityId,
                reducer.GetReducerHash()),
            Latest);
        Mock<ISnapshotCacheGrain<TProjection>> snapshot = new(MockBehavior.Strict);
        snapshot.Setup(item => item.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(restored);
        Mock<ISnapshotGrainFactory> snapshots = new(MockBehavior.Strict);
        snapshots.Setup(item => item.GetSnapshotCacheGrain<TProjection>(expected)).Returns(snapshot.Object);
        UxProjectionVersionedCacheGrain<TProjection> whole = new(
            Context(key).Object,
            snapshots.Object,
            reducer,
            NullLogger<UxProjectionVersionedCacheGrain<TProjection>>.Instance);
        await whole.OnActivateAsync(CancellationToken.None);
        wholeCaches.Add(Latest, whole);
        ReferenceSetStateAccess<TProjection, TSource> registered =
            Provider.GetRequiredService<ReferenceSetStateAccess<TProjection, TSource>>();
        ReferenceSetStateAccess<TProjection, TSource> counted = new(
            projection =>
            {
                IndexPreparations++;
                return registered.Read(projection);
            },
            registered.Write);
        ReferenceSetPageCacheGrain<TProjection, TSource> page = new(
            Context(key).Object,
            Provider.GetRequiredService<IUxProjectionGrainFactory>(),
            counted,
            NullLogger<ReferenceSetPageCacheGrain<TProjection, TSource>>.Instance);
        await page.OnActivateAsync(CancellationToken.None);
        pageCaches.Add(Latest, page);
        snapshots.VerifyAll();
        snapshot.Verify(item => item.GetStateAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Creates an actual ordinary UX grain for compatibility assertions.</summary>
    /// <returns>The activated ordinary reader.</returns>
    public async Task<IUxProjectionGrain<TProjection>> OrdinaryAsync()
    {
        UxProjectionGrain<TProjection> ordinary = new(
            Context(EntityId).Object,
            Provider.GetRequiredService<IUxProjectionGrainFactory>(),
            NullLogger<UxProjectionGrain<TProjection>>.Instance);
        await ordinary.OnActivateAsync(CancellationToken.None);
        return ordinary;
    }

    /// <summary>Checks that a resolved cache belongs to this named collection.</summary>
    /// <param name="text">The resolved cache key.</param>
    /// <returns>The verified versioned key.</returns>
    private UxProjectionVersionedCacheKey ValidateKey(
        string text
    )
    {
        UxProjectionVersionedCacheKey key = UxProjectionVersionedCacheKey.Parse(text);
        Assert.Equal(BrookNameHelper.GetBrookName<TProjection>(), key.BrookName);
        Assert.Equal(EntityId, key.EntityId);
        return key;
    }
}