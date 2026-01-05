using System;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Ripples.Abstractions;

using NSubstitute;


namespace Mississippi.Ripples.Blazor.L0Tests;

/// <summary>
///     Unit tests for <see cref="ProjectionCache" />.
/// </summary>
[AllureParentSuite("Ripples")]
[AllureSuite("Blazor")]
[AllureSubSuite("Components")]
public sealed class ProjectionCacheTests : IAsyncDisposable
{
    private readonly IRippleFactory<TestProjection> mockFactory;

    private readonly IRipple<TestProjection> mockRipple;

    private readonly IServiceProvider serviceProvider;

    private readonly ProjectionCache sut;

    public ProjectionCacheTests()
    {
        mockRipple = Substitute.For<IRipple<TestProjection>>();
        mockFactory = Substitute.For<IRippleFactory<TestProjection>>();
        mockFactory.Create().Returns(mockRipple);
        ServiceCollection services = new();
        services.AddSingleton(mockFactory);
        serviceProvider = services.BuildServiceProvider();
        sut = new(serviceProvider);
    }

    public async ValueTask DisposeAsync()
    {
        await sut.DisposeAsync();
    }

    /// <summary>
    ///     Test projection type.
    /// </summary>
    public sealed record TestProjection(string Name);

    [Fact]
    public void ConstructorThrowsWhenServiceProviderIsNull()
    {
        // Act
        Action act = () => _ = new ProjectionCache(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
    }

    [Fact]
    public async Task DisposingOnlyFirstSubscriptionKeepsDataForSecond()
    {
        // Arrange
        const string entityId = "entity-123";
        TestProjection expectedProjection = new("Shared");
        mockRipple.Current.Returns(expectedProjection);
        mockRipple.IsLoaded.Returns(true);
        IProjectionSubscription<TestProjection> sub1 = await sut.SubscribeAsync<TestProjection>(entityId, () => { });
        IProjectionSubscription<TestProjection> sub2 = await sut.SubscribeAsync<TestProjection>(entityId, () => { });

        // Act - Dispose first subscription
        await sub1.DisposeAsync();

        // Assert - Second subscription still has data
        sub2.Current.Should().Be(expectedProjection);
    }

    [Fact]
    public async Task EvictAsyncRemovesProjectionFromCache()
    {
        // Arrange
        const string entityId = "entity-123";
        mockRipple.IsLoaded.Returns(true);
        await sut.SubscribeAsync<TestProjection>(entityId, () => { });

        // Act
        await sut.EvictAsync<TestProjection>(entityId);

        // Assert
        sut.IsCached<TestProjection>(entityId).Should().BeFalse();
    }

    [Fact]
    public async Task GetCachedReturnsDataAfterSubscriptionDisposed()
    {
        // Arrange
        const string entityId = "entity-123";
        TestProjection expectedProjection = new("Test");
        mockRipple.Current.Returns(expectedProjection);
        mockRipple.IsLoaded.Returns(true);
        IProjectionSubscription<TestProjection> subscription =
            await sut.SubscribeAsync<TestProjection>(entityId, () => { });
        await subscription.DisposeAsync();

        // Act
        TestProjection? cached = sut.GetCached<TestProjection>(entityId);

        // Assert
        cached.Should().Be(expectedProjection);
    }

    [Fact]
    public void GetCachedReturnsNullForNonCachedProjection()
    {
        // Act & Assert
        sut.GetCached<TestProjection>("non-existent").Should().BeNull();
    }

    [Fact]
    public void IsCachedReturnsFalseForNonCachedProjection()
    {
        // Act & Assert
        sut.IsCached<TestProjection>("non-existent").Should().BeFalse();
    }

    [Fact]
    public async Task IsCachedReturnsTrueForCachedProjection()
    {
        // Arrange
        const string entityId = "entity-123";
        mockRipple.IsLoaded.Returns(true);
        await sut.SubscribeAsync<TestProjection>(entityId, () => { });

        // Act & Assert
        sut.IsCached<TestProjection>(entityId).Should().BeTrue();
    }

    [Fact]
    public async Task LruEvictionDoesNotRemoveEntriesWithActiveSubscriptions()
    {
        // Arrange - Create cache with max size of 2
        ProjectionCache smallCache = new(serviceProvider, 2);

        // Create 3 different ripples
        IRipple<TestProjection> ripple1 = Substitute.For<IRipple<TestProjection>>();
        ripple1.IsLoaded.Returns(true);
        IRipple<TestProjection> ripple2 = Substitute.For<IRipple<TestProjection>>();
        ripple2.IsLoaded.Returns(true);
        IRipple<TestProjection> ripple3 = Substitute.For<IRipple<TestProjection>>();
        ripple3.IsLoaded.Returns(true);
        int callCount = 0;
        mockFactory.Create()
            .Returns(_ =>
            {
                callCount++;
                return callCount switch
                {
                    1 => ripple1,
                    2 => ripple2,
                    var _ => ripple3,
                };
            });

        // Subscribe to entity-1 (keep active) and entity-2 (dispose)
        IProjectionSubscription<TestProjection> sub1 =
            await smallCache.SubscribeAsync<TestProjection>("entity-1", () => { });
        IProjectionSubscription<TestProjection> sub2 =
            await smallCache.SubscribeAsync<TestProjection>("entity-2", () => { });
        await sub2.DisposeAsync(); // Only dispose sub2

        // Act - Subscribe to entity-3
        await smallCache.SubscribeAsync<TestProjection>("entity-3", () => { });

        // Assert - entity-1 should NOT be evicted (has active subscription)
        smallCache.IsCached<TestProjection>("entity-1").Should().BeTrue("active subscription prevents eviction");
        smallCache.IsCached<TestProjection>("entity-2").Should().BeFalse("inactive entry should be evicted");
        smallCache.IsCached<TestProjection>("entity-3").Should().BeTrue("new entry should be cached");
        await sub1.DisposeAsync();
        await smallCache.DisposeAsync();
    }

    [Fact]
    public async Task LruEvictionRemovesOldestUnusedEntry()
    {
        // Arrange - Create cache with max size of 2
        ProjectionCache smallCache = new(serviceProvider, 2);

        // Create 3 different ripples
        IRipple<TestProjection> ripple1 = Substitute.For<IRipple<TestProjection>>();
        ripple1.IsLoaded.Returns(true);
        IRipple<TestProjection> ripple2 = Substitute.For<IRipple<TestProjection>>();
        ripple2.IsLoaded.Returns(true);
        IRipple<TestProjection> ripple3 = Substitute.For<IRipple<TestProjection>>();
        ripple3.IsLoaded.Returns(true);

        // Set up factory to return different ripples
        int callCount = 0;
        mockFactory.Create()
            .Returns(_ =>
            {
                callCount++;
                return callCount switch
                {
                    1 => ripple1,
                    2 => ripple2,
                    var _ => ripple3,
                };
            });

        // Subscribe to entity-1 and entity-2, then dispose them
        IProjectionSubscription<TestProjection> sub1 =
            await smallCache.SubscribeAsync<TestProjection>("entity-1", () => { });
        IProjectionSubscription<TestProjection> sub2 =
            await smallCache.SubscribeAsync<TestProjection>("entity-2", () => { });
        await sub1.DisposeAsync();
        await sub2.DisposeAsync();

        // Act - Subscribe to entity-3, should trigger eviction of entity-1 (oldest)
        await smallCache.SubscribeAsync<TestProjection>("entity-3", () => { });

        // Assert
        smallCache.IsCached<TestProjection>("entity-1").Should().BeFalse("oldest entry should be evicted");
        smallCache.IsCached<TestProjection>("entity-2").Should().BeTrue("second entry should remain");
        smallCache.IsCached<TestProjection>("entity-3").Should().BeTrue("new entry should be cached");
        await smallCache.DisposeAsync();
    }

    [Fact]
    public async Task MultipleSubscriptionsToSameEntityShareCachedData()
    {
        // Arrange
        const string entityId = "entity-123";
        TestProjection expectedProjection = new("Shared");
        mockRipple.Current.Returns(expectedProjection);
        mockRipple.IsLoaded.Returns(true);

        // Act - Two subscriptions to same entity
        IProjectionSubscription<TestProjection> sub1 = await sut.SubscribeAsync<TestProjection>(entityId, () => { });
        IProjectionSubscription<TestProjection> sub2 = await sut.SubscribeAsync<TestProjection>(entityId, () => { });

        // Assert - Both see same data
        sub1.Current.Should().Be(expectedProjection);
        sub2.Current.Should().Be(expectedProjection);

        // Factory only called once
        mockFactory.Received(1).Create();
    }

    [Fact]
    public async Task OnChangedCallbackIsInvokedWhenRippleChanges()
    {
        // Arrange
        const string entityId = "entity-123";
        int callbackCount = 0;
        mockRipple.IsLoaded.Returns(true);
        await sut.SubscribeAsync<TestProjection>(entityId, () => callbackCount++);

        // Act - Simulate ripple change
        mockRipple.Changed += Raise.Event<EventHandler>(mockRipple, EventArgs.Empty);

        // Assert
        callbackCount.Should().Be(1);
    }

    [Fact]
    public async Task OnChangedCallbackNotInvokedAfterSubscriptionDisposed()
    {
        // Arrange
        const string entityId = "entity-123";
        int callbackCount = 0;
        mockRipple.IsLoaded.Returns(true);
        IProjectionSubscription<TestProjection> subscription =
            await sut.SubscribeAsync<TestProjection>(entityId, () => callbackCount++);
        await subscription.DisposeAsync();

        // Act - Simulate ripple change after dispose
        mockRipple.Changed += Raise.Event<EventHandler>(mockRipple, EventArgs.Empty);

        // Assert - Callback should NOT be invoked
        callbackCount.Should().Be(0);
    }

    [Fact]
    public async Task RefreshAsyncCallsRippleRefresh()
    {
        // Arrange
        const string entityId = "entity-123";
        mockRipple.IsLoaded.Returns(true);
        await sut.SubscribeAsync<TestProjection>(entityId, () => { });

        // Act
        await sut.RefreshAsync<TestProjection>(entityId);

        // Assert
        await mockRipple.Received(1).RefreshAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubscribeAsyncCallsRippleSubscribeWhenNotLoaded()
    {
        // Arrange
        const string entityId = "entity-123";
        mockRipple.IsLoaded.Returns(false);
        mockRipple.IsLoading.Returns(false);

        // Act
        await sut.SubscribeAsync<TestProjection>(entityId, () => { });

        // Assert
        await mockRipple.Received(1).SubscribeAsync(entityId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubscribeAsyncCreatesRippleViaFactory()
    {
        // Arrange
        const string entityId = "entity-123";

        // Act
        IProjectionSubscription<TestProjection> subscription =
            await sut.SubscribeAsync<TestProjection>(entityId, () => { });

        // Assert
        mockFactory.Received(1).Create();
        subscription.EntityId.Should().Be(entityId);
    }

    [Fact]
    public async Task SubscribeAsyncReturnsSubscriptionWithCorrectProperties()
    {
        // Arrange
        const string entityId = "entity-123";
        TestProjection expectedProjection = new("Test");
        mockRipple.Current.Returns(expectedProjection);
        mockRipple.IsLoading.Returns(false);
        mockRipple.IsLoaded.Returns(true);

        // Act
        IProjectionSubscription<TestProjection> subscription =
            await sut.SubscribeAsync<TestProjection>(entityId, () => { });

        // Assert
        subscription.Current.Should().Be(expectedProjection);
        subscription.IsLoading.Should().BeFalse();
        subscription.IsLoaded.Should().BeTrue();
    }

    [Fact]
    public async Task SubscribeAsyncReusesCachedRippleOnSecondCall()
    {
        // Arrange
        const string entityId = "entity-123";
        mockRipple.IsLoaded.Returns(true);

        // Act - First subscription
        await sut.SubscribeAsync<TestProjection>(entityId, () => { });

        // Act - Second subscription (e.g., navigating back)
        await sut.SubscribeAsync<TestProjection>(entityId, () => { });

        // Assert - Factory only called once
        mockFactory.Received(1).Create();
    }

    [Fact]
    public async Task SubscribeAsyncThrowsWhenDisposed()
    {
        // Arrange
        await sut.DisposeAsync();

        // Act
        Func<Task> act = async () => await sut.SubscribeAsync<TestProjection>("entity-123", () => { });

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task SubscribeAsyncThrowsWhenEntityIdIsEmpty()
    {
        // Act
        Func<Task> act = async () => await sut.SubscribeAsync<TestProjection>(string.Empty, () => { });

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("entityId");
    }

    [Fact]
    public async Task SubscribeAsyncThrowsWhenEntityIdIsNull()
    {
        // Act
        Func<Task> act = async () => await sut.SubscribeAsync<TestProjection>(null!, () => { });

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("entityId");
    }

    [Fact]
    public async Task SubscribeAsyncThrowsWhenOnChangedIsNull()
    {
        // Act
        Func<Task> act = async () => await sut.SubscribeAsync<TestProjection>("entity-123", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("onChanged");
    }

    [Fact]
    public async Task SubscriptionDisposeRemovesCallbackButKeepsData()
    {
        // Arrange
        const string entityId = "entity-123";
        int callbackCount = 0;
        mockRipple.IsLoaded.Returns(true);
        IProjectionSubscription<TestProjection> subscription =
            await sut.SubscribeAsync<TestProjection>(entityId, () => callbackCount++);

        // Act - Dispose the subscription
        await subscription.DisposeAsync();

        // Assert - Data is still cached
        sut.IsCached<TestProjection>(entityId).Should().BeTrue();
    }
}