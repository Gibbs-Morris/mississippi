using System;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using FluentAssertions;

using Mississippi.Ripples.Abstractions;

using NSubstitute;


namespace Mississippi.Ripples.Blazor.L0Tests;

/// <summary>
///     Unit tests for <see cref="ProjectionBinder{TProjection}" />.
/// </summary>
[AllureParentSuite("Ripples")]
[AllureSuite("Blazor")]
[AllureSubSuite("Components")]
public sealed class ProjectionBinderTests : IAsyncDisposable
{
    private readonly IProjectionCache mockCache;

    private readonly IProjectionSubscription<TestProjection> mockSubscription;

    private readonly ProjectionBinder<TestProjection> sut;

    public ProjectionBinderTests()
    {
        mockCache = Substitute.For<IProjectionCache>();
        mockSubscription = Substitute.For<IProjectionSubscription<TestProjection>>();
        mockCache.SubscribeAsync<TestProjection>(Arg.Any<string>(), Arg.Any<Action>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockSubscription));
        sut = new(mockCache);
    }

    public async ValueTask DisposeAsync()
    {
        await sut.DisposeAsync();
    }

    [Fact]
    public async Task BindAsyncThrowsWhenEntityIdIsEmpty()
    {
        // Act
        Func<Task> act = async () => await sut.BindAsync(string.Empty, () => { });

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("entityId");
    }

    [Fact]
    public async Task BindAsyncThrowsWhenEntityIdIsNull()
    {
        // Act
        Func<Task> act = async () => await sut.BindAsync(null!, () => { });

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("entityId");
    }

    [Fact]
    public async Task BindAsyncThrowsWhenOnChangedIsNull()
    {
        // Act
        Func<Task> act = async () => await sut.BindAsync("entity-1", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("onChanged");
    }

    [Fact]
    public async Task BindAsync_AfterDispose_Throws()
    {
        // Arrange
        await sut.DisposeAsync();

        // Act
        Func<Task> act = async () => await sut.BindAsync("entity-1", () => { });

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    // BindAsync Tests - Same Entity (No-op)
    [Fact]
    public async Task BindAsync_CalledTwiceWithSameEntityId_OnlySubscribesOnce()
    {
        // Arrange
        const string entityId = "user-123";
        Action onChanged = () => { };

        // Act
        await sut.BindAsync(entityId, onChanged);
        await sut.BindAsync(entityId, onChanged);

        // Assert
        await mockCache.Received(1)
            .SubscribeAsync<TestProjection>(entityId, Arg.Any<Action>(), Arg.Any<CancellationToken>());
    }

    // BindAsync Tests - Different Entity (Switch)
    [Fact]
    public async Task BindAsync_ChangingEntityId_DisposesOldSubscription()
    {
        // Arrange
        const string entityId1 = "user-1";
        const string entityId2 = "user-2";
        IProjectionSubscription<TestProjection> mockSubscription1 =
            Substitute.For<IProjectionSubscription<TestProjection>>();
        IProjectionSubscription<TestProjection> mockSubscription2 =
            Substitute.For<IProjectionSubscription<TestProjection>>();
        mockCache.SubscribeAsync<TestProjection>(entityId1, Arg.Any<Action>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockSubscription1));
        mockCache.SubscribeAsync<TestProjection>(entityId2, Arg.Any<Action>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockSubscription2));

        // Act
        await sut.BindAsync(entityId1, () => { });
        await sut.BindAsync(entityId2, () => { });

        // Assert
        await mockSubscription1.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task BindAsync_ChangingEntityId_SubscribesToNewEntity()
    {
        // Arrange
        const string entityId1 = "user-1";
        const string entityId2 = "user-2";

        // Act
        await sut.BindAsync(entityId1, () => { });
        await sut.BindAsync(entityId2, () => { });

        // Assert
        await mockCache.Received(1)
            .SubscribeAsync<TestProjection>(entityId1, Arg.Any<Action>(), Arg.Any<CancellationToken>());
        await mockCache.Received(1)
            .SubscribeAsync<TestProjection>(entityId2, Arg.Any<Action>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BindAsync_ChangingEntityId_UpdatesEntityIdProperty()
    {
        // Arrange
        const string entityId1 = "user-1";
        const string entityId2 = "user-2";

        // Act
        await sut.BindAsync(entityId1, () => { });
        await sut.BindAsync(entityId2, () => { });

        // Assert
        sut.EntityId.Should().Be(entityId2);
    }

    [Fact]
    public async Task BindAsync_WithValidEntityId_SetsEntityIdProperty()
    {
        // Arrange
        const string entityId = "user-123";

        // Act
        await sut.BindAsync(entityId, () => { });

        // Assert
        sut.EntityId.Should().Be(entityId);
    }

    // BindAsync Tests - Basic
    [Fact]
    public async Task BindAsync_WithValidEntityId_SubscribesToCache()
    {
        // Arrange
        const string entityId = "user-123";
        Action onChanged = () => { };

        // Act
        await sut.BindAsync(entityId, onChanged);

        // Assert
        await mockCache.Received(1).SubscribeAsync<TestProjection>(entityId, onChanged, Arg.Any<CancellationToken>());
    }

    // Constructor Tests
    [Fact]
    public void ConstructorThrowsWhenCacheIsNull()
    {
        // Act
        Action act = () => _ = new ProjectionBinder<TestProjection>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    // Property Delegation Tests
    [Fact]
    public async Task CurrentDelegatesToSubscription()
    {
        // Arrange
        TestProjection expected = new("Test", 42);
        mockSubscription.Current.Returns(expected);
        await sut.BindAsync("entity-1", () => { });

        // Act
        TestProjection? result = sut.Current;

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void CurrentReturnsNullWhenNotBound()
    {
        // Act
        TestProjection? result = sut.Current;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_OnlyDisposesOnce()
    {
        // Arrange
        await sut.BindAsync("entity-1", () => { });

        // Act
        await sut.DisposeAsync();
        await sut.DisposeAsync();

        // Assert
        await mockSubscription.Received(1).DisposeAsync();
    }

    // DisposeAsync Tests
    [Fact]
    public async Task DisposeAsync_DisposesActiveSubscription()
    {
        // Arrange
        await sut.BindAsync("entity-1", () => { });

        // Act
        await sut.DisposeAsync();

        // Assert
        await mockSubscription.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_SetsEntityIdToNull()
    {
        // Arrange
        await sut.BindAsync("entity-1", () => { });

        // Act
        await sut.DisposeAsync();

        // Assert
        sut.EntityId.Should().BeNull();
    }

    [Fact]
    public async Task DisposeAsync_WhenNotBound_DoesNotThrow()
    {
        // Act
        Func<Task> act = async () => await sut.DisposeAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task IsLoadedDelegatesToSubscription()
    {
        // Arrange
        mockSubscription.IsLoaded.Returns(true);
        await sut.BindAsync("entity-1", () => { });

        // Act
        bool result = sut.IsLoaded;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsLoadedReturnsFalseWhenNotBound()
    {
        // Act
        bool result = sut.IsLoaded;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsLoadingDelegatesToSubscription()
    {
        // Arrange
        mockSubscription.IsLoading.Returns(true);
        await sut.BindAsync("entity-1", () => { });

        // Act
        bool result = sut.IsLoading;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsLoadingReturnsFalseWhenNotBound()
    {
        // Act
        bool result = sut.IsLoading;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task LastErrorDelegatesToSubscription()
    {
        // Arrange
        Exception expected = new InvalidOperationException("Test error");
        mockSubscription.LastError.Returns(expected);
        await sut.BindAsync("entity-1", () => { });

        // Act
        Exception? result = sut.LastError;

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void LastErrorReturnsNullWhenNotBound()
    {
        // Act
        Exception? result = sut.LastError;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UnbindAsync_AllowsRebinding()
    {
        // Arrange
        const string entityId1 = "entity-1";
        const string entityId2 = "entity-2";
        IProjectionSubscription<TestProjection> mockSub1 = Substitute.For<IProjectionSubscription<TestProjection>>();
        IProjectionSubscription<TestProjection> mockSub2 = Substitute.For<IProjectionSubscription<TestProjection>>();
        mockCache.SubscribeAsync<TestProjection>(entityId1, Arg.Any<Action>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockSub1));
        mockCache.SubscribeAsync<TestProjection>(entityId2, Arg.Any<Action>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mockSub2));
        await sut.BindAsync(entityId1, () => { });
        await sut.UnbindAsync();

        // Act
        await sut.BindAsync(entityId2, () => { });

        // Assert
        sut.EntityId.Should().Be(entityId2);
    }

    // UnbindAsync Tests
    [Fact]
    public async Task UnbindAsync_DisposesSubscription()
    {
        // Arrange
        await sut.BindAsync("entity-1", () => { });

        // Act
        await sut.UnbindAsync();

        // Assert
        await mockSubscription.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task UnbindAsync_SetsEntityIdToNull()
    {
        // Arrange
        await sut.BindAsync("entity-1", () => { });

        // Act
        await sut.UnbindAsync();

        // Assert
        sut.EntityId.Should().BeNull();
    }

    [Fact]
    public async Task UnbindAsync_WhenNotBound_DoesNotThrow()
    {
        // Act
        Func<Task> act = async () => await sut.UnbindAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }
}