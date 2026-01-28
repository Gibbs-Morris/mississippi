using System;


using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Client.L0Tests;

/// <summary>
///     Tests for <see cref="ProjectionCacheMiddleware" />.
/// </summary>
public sealed class ProjectionCacheMiddlewareTests
{
    /// <summary>
    ///     Test action for non-projection action handling.
    /// </summary>
    private sealed record TestAction : IAction;

    /// <summary>
    ///     Test projection record for unit tests.
    /// </summary>
    private sealed record TestProjection(string Name);

    /// <summary>
    ///     Constructor should throw when projectionCache is null.
    /// </summary>
    [Fact]
        public void ConstructorThrowsWhenProjectionCacheIsNull() =>
        Assert.Throws<ArgumentNullException>(() => new ProjectionCacheMiddleware(null!));

    /// <summary>
    ///     Invoke should call next middleware for any action.
    /// </summary>
    [Fact]
        public void InvokeCallsNextMiddleware()
    {
        // Arrange
        ProjectionCache cache = new();
        ProjectionCacheMiddleware sut = new(cache);
        ProjectionLoadingAction<TestProjection> action = new("entity-1");
        bool nextCalled = false;

        // Act
        sut.Invoke(action, _ => nextCalled = true);

        // Assert
        Assert.True(nextCalled);
    }

    /// <summary>
    ///     Invoke should handle ProjectionLoadedAction with null data.
    /// </summary>
    [Fact]
        public void InvokeHandlesLoadedActionWithNullData()
    {
        // Arrange
        ProjectionCache cache = new();
        ProjectionCacheMiddleware sut = new(cache);
        ProjectionLoadedAction<TestProjection> action = new("entity-1", null, 3L);

        // Act
        sut.Invoke(action, _ => { });

        // Assert
        Assert.Null(cache.GetProjection<TestProjection>("entity-1"));
        Assert.Equal(3L, cache.GetProjectionVersion<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     Invoke should handle ProjectionUpdatedAction with null data.
    /// </summary>
    [Fact]
        public void InvokeHandlesUpdatedActionWithNullData()
    {
        // Arrange
        ProjectionCache cache = new();
        ProjectionCacheMiddleware sut = new(cache);
        ProjectionUpdatedAction<TestProjection> action = new("entity-1", null, 7L);

        // Act
        sut.Invoke(action, _ => { });

        // Assert
        Assert.Null(cache.GetProjection<TestProjection>("entity-1"));
        Assert.Equal(7L, cache.GetProjectionVersion<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     Invoke should handle ProjectionConnectionChangedAction without cache update.
    /// </summary>
    [Fact]
        public void InvokePassesThroughConnectionChangedAction()
    {
        // Arrange
        ProjectionCache cache = new();
        ProjectionCacheMiddleware sut = new(cache);
        ProjectionConnectionChangedAction<TestProjection> action = new("entity-1", true);
        bool nextCalled = false;

        // Act
        sut.Invoke(action, _ => nextCalled = true);

        // Assert - connection not set by middleware (that's done by ProjectionNotifier)
        Assert.True(nextCalled);
        Assert.False(cache.IsProjectionConnected<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     Invoke should handle ProjectionErrorAction without cache update.
    /// </summary>
    [Fact]
        public void InvokePassesThroughErrorAction()
    {
        // Arrange
        ProjectionCache cache = new();
        ProjectionCacheMiddleware sut = new(cache);
        ProjectionErrorAction<TestProjection> action = new("entity-1", new InvalidOperationException("Error"));
        bool nextCalled = false;

        // Act
        sut.Invoke(action, _ => nextCalled = true);

        // Assert - error not set by middleware (that's done by ProjectionNotifier)
        Assert.True(nextCalled);
        Assert.Null(cache.GetProjectionError<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     Invoke should pass through non-projection actions without modification.
    /// </summary>
    [Fact]
        public void InvokePassesThroughNonProjectionActions()
    {
        // Arrange
        ProjectionCache cache = new();
        ProjectionCacheMiddleware sut = new(cache);
        TestAction action = new();
        IAction? passedAction = null;

        // Act
        sut.Invoke(action, a => passedAction = a);

        // Assert
        Assert.Same(action, passedAction);
    }

    /// <summary>
    ///     Invoke should handle RefreshProjectionAction without cache update.
    /// </summary>
    [Fact]
        public void InvokePassesThroughRefreshAction()
    {
        // Arrange
        ProjectionCache cache = new();
        ProjectionCacheMiddleware sut = new(cache);
        RefreshProjectionAction<TestProjection> action = new("entity-1");
        bool nextCalled = false;

        // Act
        sut.Invoke(action, _ => nextCalled = true);

        // Assert
        Assert.True(nextCalled);
    }

    /// <summary>
    ///     Invoke should handle SubscribeToProjectionAction without cache update.
    /// </summary>
    [Fact]
        public void InvokePassesThroughSubscribeAction()
    {
        // Arrange
        ProjectionCache cache = new();
        ProjectionCacheMiddleware sut = new(cache);
        SubscribeToProjectionAction<TestProjection> action = new("entity-1");
        bool nextCalled = false;

        // Act
        sut.Invoke(action, _ => nextCalled = true);

        // Assert
        Assert.True(nextCalled);
    }

    /// <summary>
    ///     Invoke should handle UnsubscribeFromProjectionAction without cache update.
    /// </summary>
    [Fact]
        public void InvokePassesThroughUnsubscribeAction()
    {
        // Arrange
        ProjectionCache cache = new();
        ProjectionCacheMiddleware sut = new(cache);
        UnsubscribeFromProjectionAction<TestProjection> action = new("entity-1");
        bool nextCalled = false;

        // Act
        sut.Invoke(action, _ => nextCalled = true);

        // Assert
        Assert.True(nextCalled);
    }

    /// <summary>
    ///     Invoke should call SetLoaded on ProjectionLoadedAction.
    /// </summary>
    [Fact]
        public void InvokeSetsLoadedOnProjectionLoadedAction()
    {
        // Arrange
        ProjectionCache cache = new();
        ProjectionCacheMiddleware sut = new(cache);
        TestProjection data = new("Test");
        ProjectionLoadedAction<TestProjection> action = new("entity-1", data, 5L);

        // Act
        sut.Invoke(action, _ => { });

        // Assert
        TestProjection? result = cache.GetProjection<TestProjection>("entity-1");
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal(5L, cache.GetProjectionVersion<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     Invoke should call SetLoading on ProjectionLoadingAction.
    /// </summary>
    [Fact]
        public void InvokeSetsLoadingOnProjectionLoadingAction()
    {
        // Arrange
        ProjectionCache cache = new();
        ProjectionCacheMiddleware sut = new(cache);
        ProjectionLoadingAction<TestProjection> action = new("entity-1");

        // Act
        sut.Invoke(action, _ => { });

        // Assert
        Assert.True(cache.IsProjectionLoading<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     Invoke should call SetUpdated on ProjectionUpdatedAction.
    /// </summary>
    [Fact]
        public void InvokeSetsUpdatedOnProjectionUpdatedAction()
    {
        // Arrange
        ProjectionCache cache = new();
        ProjectionCacheMiddleware sut = new(cache);
        TestProjection data = new("Updated");
        ProjectionUpdatedAction<TestProjection> action = new("entity-1", data, 10L);

        // Act
        sut.Invoke(action, _ => { });

        // Assert
        TestProjection? result = cache.GetProjection<TestProjection>("entity-1");
        Assert.NotNull(result);
        Assert.Equal("Updated", result.Name);
        Assert.Equal(10L, cache.GetProjectionVersion<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     Invoke should throw when action is null.
    /// </summary>
    [Fact]
        public void InvokeThrowsWhenActionIsNull()
    {
        // Arrange
        ProjectionCache cache = new();
        ProjectionCacheMiddleware sut = new(cache);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.Invoke(null!, _ => { }));
    }

    /// <summary>
    ///     Invoke should throw when nextAction is null.
    /// </summary>
    [Fact]
        public void InvokeThrowsWhenNextActionIsNull()
    {
        // Arrange
        ProjectionCache cache = new();
        ProjectionCacheMiddleware sut = new(cache);
        ProjectionLoadingAction<TestProjection> action = new("entity-1");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.Invoke(action, null!));
    }
}