using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Mississippi.Ripples.Abstractions;


namespace Mississippi.Ripples.L0Tests;

/// <summary>
///     Unit tests for <see cref="RipplePool{T}" />.
/// </summary>
[AllureParentSuite("Mississippi")]
[AllureSuite("Ripples")]
[AllureSubSuite("RipplePool")]
[SuppressMessage(
    "IDisposableAnalyzers.Correctness",
    "IDISP006:Implement IDisposable",
    Justification = "Uses IAsyncLifetime for async disposal in xUnit")]
[SuppressMessage(
    "Reliability",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "Uses IAsyncLifetime for async disposal in xUnit")]
public sealed class RipplePoolTests : IAsyncLifetime
{
    private readonly TestRippleFactory factory;

    private readonly RipplePool<TestProjection> sut;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RipplePoolTests" /> class.
    /// </summary>
    public RipplePoolTests()
    {
        factory = new();
        sut = new(factory, new());
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await sut.DisposeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    ///     Test projection record for unit tests.
    /// </summary>
    /// <param name="Name">The projection name.</param>
    /// <param name="Value">The projection value.</param>
    private sealed record TestProjection(string Name, int Value = 0);

    /// <summary>
    ///     Test ripple implementation for unit tests.
    /// </summary>
    [SuppressMessage(
        "StyleCop.CSharp.MaintainabilityRules",
        "SA1119:Statement must not use unnecessary parenthesis",
        Justification = "Test stub")]
    private sealed class TestRipple : IRipple<TestProjection>
    {
        private EventHandler<RippleErrorEventArgs>? errorOccurred;

        public event EventHandler? Changed;

        public event EventHandler<RippleErrorEventArgs>? ErrorOccurred
        {
            add => errorOccurred += value;
            remove => errorOccurred -= value;
        }

        public TestProjection? Current { get; private set; }

        public bool IsConnected { get; private set; }

        public bool IsDisposed { get; private set; }

        public bool IsLoaded { get; private set; }

        public bool IsLoading { get; private set; }

        public Exception? LastError => null;

        public long? Version { get; private set; }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }

        public Task RefreshAsync(
            CancellationToken cancellationToken = default
        ) =>
            Task.CompletedTask;

        public Task SubscribeAsync(
            string entityId,
            CancellationToken cancellationToken = default
        )
        {
            IsLoading = false;
            IsLoaded = true;
            IsConnected = true;
            Current = new("Test", 1);
            Version = 1;
            Changed?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public Task UnsubscribeAsync(
            CancellationToken cancellationToken = default
        )
        {
            IsConnected = false;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Test factory that tracks Create calls.
    /// </summary>
    private sealed class TestRippleFactory : IRippleFactory<TestProjection>
    {
        public int CreateCallCount { get; private set; }

        public IRipple<TestProjection> Create()
        {
            CreateCallCount++;
            return new TestRipple();
        }
    }

    /// <summary>
    ///     DisposeAsync should dispose all pooled ripples.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Disposal")]
    public async Task DisposeAsyncDisposesAllRipples()
    {
        // Arrange
        IRipple<TestProjection> ripple1 = sut.GetOrCreate("entity-1");
        IRipple<TestProjection> ripple2 = sut.GetOrCreate("entity-2");

        // Act
        await sut.DisposeAsync();

        // Assert - all ripples should be disposed
        Assert.True(((TestRipple)ripple1).IsDisposed);
        Assert.True(((TestRipple)ripple2).IsDisposed);
    }

    /// <summary>
    ///     GetOrCreate after entity is in Warm tier should return cached and increment CacheHits.
    /// </summary>
    [Fact]
    [AllureFeature("Caching")]
    public void GetOrCreateOnWarmEntityIncrementsCacheHits()
    {
        // Arrange - create, make visible, then hidden (now in Warm)
        _ = sut.GetOrCreate("entity-1");
        sut.MarkVisible("entity-1");
        sut.MarkHidden("entity-1");
        int initialCacheHits = sut.Stats.CacheHits;

        // Act - access again
        _ = sut.GetOrCreate("entity-1");

        // Assert - should hit cache
        Assert.Equal(initialCacheHits + 1, sut.Stats.CacheHits);
        Assert.Equal(1, factory.CreateCallCount); // No new creation
    }

    /// <summary>
    ///     GetOrCreate should return different ripples for different entityIds.
    /// </summary>
    [Fact]
    [AllureFeature("Pool Management")]
    public void GetOrCreateReturnsDifferentRipplesForDifferentEntityIds()
    {
        // Act
        IRipple<TestProjection> ripple1 = sut.GetOrCreate("entity-1");
        IRipple<TestProjection> ripple2 = sut.GetOrCreate("entity-2");

        // Assert
        Assert.NotSame(ripple1, ripple2);
        Assert.Equal(2, factory.CreateCallCount);
    }

    /// <summary>
    ///     GetOrCreate should return a new ripple when called for the first time.
    /// </summary>
    [Fact]
    [AllureFeature("Pool Management")]
    public void GetOrCreateReturnsNewRippleForFirstCall()
    {
        // Act
        IRipple<TestProjection> ripple = sut.GetOrCreate("entity-1");

        // Assert
        Assert.NotNull(ripple);
        Assert.Equal(1, factory.CreateCallCount);
    }

    /// <summary>
    ///     GetOrCreate should return the same ripple for subsequent calls with the same entityId.
    /// </summary>
    [Fact]
    [AllureFeature("Pool Management")]
    public void GetOrCreateReturnsSameRippleForSameEntityId()
    {
        // Arrange
        IRipple<TestProjection> firstRipple = sut.GetOrCreate("entity-1");

        // Act
        IRipple<TestProjection> secondRipple = sut.GetOrCreate("entity-1");

        // Assert
        Assert.Same(firstRipple, secondRipple);
        Assert.Equal(1, factory.CreateCallCount);
    }

    /// <summary>
    ///     MarkHidden should move entity from Hot to Warm tier.
    /// </summary>
    [Fact]
    [AllureFeature("Tiered Subscriptions")]
    public void MarkHiddenMovesFromHotToWarm()
    {
        // Arrange
        _ = sut.GetOrCreate("entity-1");
        sut.MarkVisible("entity-1");

        // Act
        sut.MarkHidden("entity-1");

        // Assert
        Assert.Equal(0, sut.Stats.HotCount);
        Assert.Equal(1, sut.Stats.WarmCount);
    }

    /// <summary>
    ///     MarkHidden on non-visible entity should be safe.
    /// </summary>
    [Fact]
    [AllureFeature("Tiered Subscriptions")]
    public void MarkHiddenOnNonVisibleEntityIsSafe()
    {
        // Arrange
        _ = sut.GetOrCreate("entity-1");

        // Act - should not throw
        sut.MarkHidden("entity-1");

        // Assert
        Assert.Equal(0, sut.Stats.HotCount);
    }

    /// <summary>
    ///     MarkVisible should increment HotCount in stats.
    /// </summary>
    [Fact]
    [AllureFeature("Tiered Subscriptions")]
    public void MarkVisibleIncrementsHotCount()
    {
        // Arrange
        _ = sut.GetOrCreate("entity-1");

        // Act
        sut.MarkVisible("entity-1");

        // Assert
        Assert.Equal(1, sut.Stats.HotCount);
        Assert.Equal(0, sut.Stats.WarmCount);
    }

    /// <summary>
    ///     MarkVisible on already visible entity should be idempotent.
    /// </summary>
    [Fact]
    [AllureFeature("Tiered Subscriptions")]
    public void MarkVisibleIsIdempotent()
    {
        // Arrange
        _ = sut.GetOrCreate("entity-1");

        // Act
        sut.MarkVisible("entity-1");
        sut.MarkVisible("entity-1");

        // Assert
        Assert.Equal(1, sut.Stats.HotCount);
    }

    /// <summary>
    ///     PrefetchAsync should request data for all specified entities.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Batch Operations")]
    public async Task PrefetchAsyncCreatesRipplesForAllEntities()
    {
        // Arrange
        string[] entityIds = new[] { "entity-1", "entity-2", "entity-3" };

        // Act
        await sut.PrefetchAsync(entityIds);

        // Assert - should have created ripples for all
        Assert.Equal(3, factory.CreateCallCount);
        Assert.Equal(3, sut.Stats.TotalFetches);
    }

    /// <summary>
    ///     PrefetchAsync should skip already cached entities.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Batch Operations")]
    public async Task PrefetchAsyncSkipsAlreadyCachedEntities()
    {
        // Arrange - pre-create one entity
        _ = sut.GetOrCreate("entity-1");
        int initialFetchCount = sut.Stats.TotalFetches;

        // Act
        await sut.PrefetchAsync(["entity-1", "entity-2"]);

        // Assert - should only fetch entity-2
        Assert.Equal(2, factory.CreateCallCount);
        Assert.Equal(initialFetchCount + 1, sut.Stats.TotalFetches);
    }

    /// <summary>
    ///     Stats should start with all zeros.
    /// </summary>
    [Fact]
    [AllureFeature("Statistics")]
    public void StatsStartsEmpty()
    {
        // Assert
        Assert.Equal(0, sut.Stats.HotCount);
        Assert.Equal(0, sut.Stats.WarmCount);
        Assert.Equal(0, sut.Stats.TotalFetches);
        Assert.Equal(0, sut.Stats.CacheHits);
    }
}