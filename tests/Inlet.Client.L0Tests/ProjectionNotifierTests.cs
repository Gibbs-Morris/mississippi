using System;

using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Reservoir;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Client.L0Tests;

/// <summary>
///     Tests for <see cref="ProjectionNotifier" />.
/// </summary>
/// <remarks>
///     <para>
///         The ProjectionNotifier follows pure Redux: it only dispatches actions.
///         State updates happen through reducers, not through the notifier.
///     </para>
/// </remarks>
public sealed class ProjectionNotifierTests : IDisposable
{
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
    ///     Constructor should throw when store is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenStoreIsNull() =>
        Assert.Throws<ArgumentNullException>(() => new ProjectionNotifier(null!));

    /// <summary>
    ///     NotifyConnectionChanged should dispatch action with connected=false.
    /// </summary>
    [Fact]
    public void NotifyConnectionChangedDispatchesActionWithConnectedFalse()
    {
        // Arrange
        IAction? dispatchedAction = null;
        store.RegisterMiddleware(new CaptureMiddleware(a => dispatchedAction = a));
        ProjectionNotifier sut = new(store);

        // Act
        sut.NotifyConnectionChanged<TestProjection>("entity-1", false);

        // Assert
        Assert.NotNull(dispatchedAction);
        Assert.IsType<ProjectionConnectionChangedAction<TestProjection>>(dispatchedAction);
        ProjectionConnectionChangedAction<TestProjection> typed =
            (ProjectionConnectionChangedAction<TestProjection>)dispatchedAction;
        Assert.Equal("entity-1", typed.EntityId);
        Assert.False(typed.IsConnected);
    }

    /// <summary>
    ///     NotifyConnectionChanged should dispatch action with connected=true.
    /// </summary>
    [Fact]
    public void NotifyConnectionChangedDispatchesActionWithConnectedTrue()
    {
        // Arrange
        IAction? dispatchedAction = null;
        store.RegisterMiddleware(new CaptureMiddleware(a => dispatchedAction = a));
        ProjectionNotifier sut = new(store);

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
    ///     NotifyConnectionChanged should throw when entityId is null.
    /// </summary>
    [Fact]
    public void NotifyConnectionChangedThrowsWhenEntityIdIsNull()
    {
        // Arrange
        ProjectionNotifier sut = new(store);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.NotifyConnectionChanged<TestProjection>(null!, true));
    }

    /// <summary>
    ///     NotifyError should dispatch action with exception.
    /// </summary>
    [Fact]
    public void NotifyErrorDispatchesAction()
    {
        // Arrange
        IAction? dispatchedAction = null;
        store.RegisterMiddleware(new CaptureMiddleware(a => dispatchedAction = a));
        ProjectionNotifier sut = new(store);
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
    public void NotifyErrorThrowsWhenEntityIdIsNull()
    {
        // Arrange
        ProjectionNotifier sut = new(store);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            sut.NotifyError<TestProjection>(null!, new InvalidOperationException("Test")));
    }

    /// <summary>
    ///     NotifyError should throw when exception is null.
    /// </summary>
    [Fact]
    public void NotifyErrorThrowsWhenExceptionIsNull()
    {
        // Arrange
        ProjectionNotifier sut = new(store);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.NotifyError<TestProjection>("entity-1", null!));
    }

    /// <summary>
    ///     NotifyProjectionUpdated should dispatch action with data and version.
    /// </summary>
    [Fact]
    public void NotifyProjectionUpdatedDispatchesAction()
    {
        // Arrange
        IAction? dispatchedAction = null;
        store.RegisterMiddleware(new CaptureMiddleware(a => dispatchedAction = a));
        ProjectionNotifier sut = new(store);
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
    ///     NotifyProjectionUpdated should dispatch action with null data.
    /// </summary>
    [Fact]
    public void NotifyProjectionUpdatedDispatchesActionWithNullData()
    {
        // Arrange
        IAction? dispatchedAction = null;
        store.RegisterMiddleware(new CaptureMiddleware(a => dispatchedAction = a));
        ProjectionNotifier sut = new(store);

        // Act
        sut.NotifyProjectionUpdated<TestProjection>("entity-1", null, 5L);

        // Assert
        Assert.NotNull(dispatchedAction);
        Assert.IsType<ProjectionUpdatedAction<TestProjection>>(dispatchedAction);
        ProjectionUpdatedAction<TestProjection> typed = (ProjectionUpdatedAction<TestProjection>)dispatchedAction;
        Assert.Equal("entity-1", typed.EntityId);
        Assert.Null(typed.Data);
        Assert.Equal(5L, typed.Version);
    }

    /// <summary>
    ///     NotifyProjectionUpdated should throw when entityId is null.
    /// </summary>
    [Fact]
    public void NotifyProjectionUpdatedThrowsWhenEntityIdIsNull()
    {
        // Arrange
        ProjectionNotifier sut = new(store);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.NotifyProjectionUpdated<TestProjection>(null!, new("Test"), 1L));
    }
}