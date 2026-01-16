using System;

using Allure.Xunit.Attributes;

using Mississippi.Inlet.Abstractions.Actions;
using Mississippi.Reservoir;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.L0Tests;

/// <summary>
///     Tests for <see cref="ProjectionNotifier" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet")]
[AllureSuite("Core")]
[AllureSubSuite("ProjectionNotifier")]
public sealed class ProjectionNotifierTests : IDisposable
{
    private readonly ProjectionCache cache = new();

    private readonly Store store = new();

    /// <inheritdoc />
    public void Dispose() => store.Dispose();

    /// <summary>
    ///     Middleware for capturing dispatched actions.
    /// </summary>
    private sealed class CaptureMiddleware : IMiddleware
    {
        private readonly Action<IAction> capture;

        public CaptureMiddleware(
            Action<IAction> capture
        ) =>
            this.capture = capture;

        public void Invoke(
            IAction action,
            Action<IAction> nextAction
        )
        {
            capture(action);
            nextAction(action);
        }
    }

    /// <summary>
    ///     Test projection record for unit tests.
    /// </summary>
    private sealed record TestProjection(string Name);

    /// <summary>
    ///     Constructor should throw when projectionCache is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void ConstructorThrowsWhenProjectionCacheIsNull() =>
        Assert.Throws<ArgumentNullException>(() => new ProjectionNotifier(store, null!));

    /// <summary>
    ///     Constructor should throw when store is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void ConstructorThrowsWhenStoreIsNull() =>
        Assert.Throws<ArgumentNullException>(() => new ProjectionNotifier(null!, cache));

    /// <summary>
    ///     NotifyConnectionChanged should dispatch action.
    /// </summary>
    [Fact]
    [AllureFeature("Action Dispatch")]
    public void NotifyConnectionChangedDispatchesAction()
    {
        // Arrange
        IAction? dispatchedAction = null;
        store.RegisterMiddleware(new CaptureMiddleware(a => dispatchedAction = a));
        ProjectionNotifier sut = new(store, cache);

        // Act
        sut.NotifyConnectionChanged<TestProjection>("entity-1", true);

        // Assert
        Assert.NotNull(dispatchedAction);
        Assert.IsType<ProjectionConnectionChangedAction<TestProjection>>(dispatchedAction);
        ProjectionConnectionChangedAction<TestProjection> typed =
            (ProjectionConnectionChangedAction<TestProjection>)dispatchedAction;
        Assert.Equal("entity-1", typed.EntityId);
        Assert.True(typed.IsConnected);
    }

    /// <summary>
    ///     NotifyConnectionChanged should handle disconnect.
    /// </summary>
    [Fact]
    [AllureFeature("Cache Updates")]
    public void NotifyConnectionChangedHandlesDisconnect()
    {
        // Arrange
        ProjectionNotifier sut = new(store, cache);
        sut.NotifyConnectionChanged<TestProjection>("entity-1", true);

        // Act
        sut.NotifyConnectionChanged<TestProjection>("entity-1", false);

        // Assert
        Assert.False(cache.IsProjectionConnected<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     NotifyConnectionChanged should throw when entityId is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void NotifyConnectionChangedThrowsWhenEntityIdIsNull()
    {
        // Arrange
        ProjectionNotifier sut = new(store, cache);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.NotifyConnectionChanged<TestProjection>(null!, true));
    }

    /// <summary>
    ///     NotifyConnectionChanged should update cache.
    /// </summary>
    [Fact]
    [AllureFeature("Cache Updates")]
    public void NotifyConnectionChangedUpdatesCache()
    {
        // Arrange
        ProjectionNotifier sut = new(store, cache);

        // Act
        sut.NotifyConnectionChanged<TestProjection>("entity-1", true);

        // Assert
        Assert.True(cache.IsProjectionConnected<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     NotifyError should dispatch action.
    /// </summary>
    [Fact]
    [AllureFeature("Action Dispatch")]
    public void NotifyErrorDispatchesAction()
    {
        // Arrange
        IAction? dispatchedAction = null;
        store.RegisterMiddleware(new CaptureMiddleware(a => dispatchedAction = a));
        ProjectionNotifier sut = new(store, cache);
        InvalidOperationException error = new("Test error");

        // Act
        sut.NotifyError<TestProjection>("entity-1", error);

        // Assert
        Assert.NotNull(dispatchedAction);
        Assert.IsType<ProjectionErrorAction<TestProjection>>(dispatchedAction);
        ProjectionErrorAction<TestProjection> typed = (ProjectionErrorAction<TestProjection>)dispatchedAction;
        Assert.Equal("entity-1", typed.EntityId);
        Assert.Same(error, typed.Error);
    }

    /// <summary>
    ///     NotifyError should throw when entityId is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void NotifyErrorThrowsWhenEntityIdIsNull()
    {
        // Arrange
        ProjectionNotifier sut = new(store, cache);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            sut.NotifyError<TestProjection>(null!, new InvalidOperationException("Test")));
    }

    /// <summary>
    ///     NotifyError should throw when exception is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void NotifyErrorThrowsWhenExceptionIsNull()
    {
        // Arrange
        ProjectionNotifier sut = new(store, cache);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.NotifyError<TestProjection>("entity-1", null!));
    }

    /// <summary>
    ///     NotifyError should update cache.
    /// </summary>
    [Fact]
    [AllureFeature("Cache Updates")]
    public void NotifyErrorUpdatesCache()
    {
        // Arrange
        ProjectionNotifier sut = new(store, cache);
        InvalidOperationException error = new("Test error");

        // Act
        sut.NotifyError<TestProjection>("entity-1", error);

        // Assert
        Exception? result = cache.GetProjectionError<TestProjection>("entity-1");
        Assert.NotNull(result);
        Assert.Equal("Test error", result.Message);
    }

    /// <summary>
    ///     NotifyProjectionUpdated should dispatch action.
    /// </summary>
    [Fact]
    [AllureFeature("Action Dispatch")]
    public void NotifyProjectionUpdatedDispatchesAction()
    {
        // Arrange
        IAction? dispatchedAction = null;
        store.RegisterMiddleware(new CaptureMiddleware(a => dispatchedAction = a));
        ProjectionNotifier sut = new(store, cache);
        TestProjection data = new("Updated");

        // Act
        sut.NotifyProjectionUpdated("entity-1", data, 10L);

        // Assert
        Assert.NotNull(dispatchedAction);
        Assert.IsType<ProjectionUpdatedAction<TestProjection>>(dispatchedAction);
        ProjectionUpdatedAction<TestProjection> typed = (ProjectionUpdatedAction<TestProjection>)dispatchedAction;
        Assert.Equal("entity-1", typed.EntityId);
        Assert.Same(data, typed.Data);
        Assert.Equal(10L, typed.Version);
    }

    /// <summary>
    ///     NotifyProjectionUpdated should handle null data.
    /// </summary>
    [Fact]
    [AllureFeature("Cache Updates")]
    public void NotifyProjectionUpdatedHandlesNullData()
    {
        // Arrange
        ProjectionNotifier sut = new(store, cache);

        // Act
        sut.NotifyProjectionUpdated<TestProjection>("entity-1", null, 5L);

        // Assert
        Assert.Null(cache.GetProjection<TestProjection>("entity-1"));
        Assert.Equal(5L, cache.GetProjectionVersion<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     NotifyProjectionUpdated should throw when entityId is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void NotifyProjectionUpdatedThrowsWhenEntityIdIsNull()
    {
        // Arrange
        ProjectionNotifier sut = new(store, cache);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.NotifyProjectionUpdated<TestProjection>(null!, new("Test"), 1L));
    }

    /// <summary>
    ///     NotifyProjectionUpdated should update cache.
    /// </summary>
    [Fact]
    [AllureFeature("Cache Updates")]
    public void NotifyProjectionUpdatedUpdatesCache()
    {
        // Arrange
        ProjectionNotifier sut = new(store, cache);
        TestProjection data = new("Updated");

        // Act
        sut.NotifyProjectionUpdated("entity-1", data, 10L);

        // Assert
        TestProjection? result = cache.GetProjection<TestProjection>("entity-1");
        Assert.NotNull(result);
        Assert.Equal("Updated", result.Name);
        Assert.Equal(10L, cache.GetProjectionVersion<TestProjection>("entity-1"));
    }
}