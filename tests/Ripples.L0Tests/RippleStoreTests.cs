using System;
using System.Diagnostics.CodeAnalysis;

using Allure.Xunit.Attributes;

using Mississippi.Ripples.Abstractions.Actions;
using Mississippi.Ripples.Abstractions.State;


namespace Mississippi.Ripples.L0Tests;

/// <summary>
///     Tests for <see cref="RippleStore" />.
/// </summary>
[AllureParentSuite("Mississippi.Ripples")]
[AllureSuite("Core")]
[AllureSubSuite("RippleStore")]
public sealed class RippleStoreTests : IDisposable
{
    private readonly RippleStore sut;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RippleStoreTests" /> class.
    /// </summary>
    public RippleStoreTests() => sut = new();

    /// <inheritdoc />
    public void Dispose()
    {
        sut.Dispose();
    }

    /// <summary>
    ///     Test projection record for unit tests.
    /// </summary>
    /// <param name="Name">The projection name.</param>
    /// <param name="Value">The projection value.</param>
    private sealed record TestProjection(string Name, int Value = 0);

    /// <summary>
    ///     ConnectionChanged action should be ignored when dispatched for non-existent entity.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void ConnectionChangedActionForNonExistentEntityDoesNotCreateState()
    {
        // Arrange
        ProjectionConnectionChangedAction<TestProjection> connectedAction = new("non-existent", true);

        // Act
        sut.Dispatch(connectedAction);
        IProjectionState<TestProjection>? state = sut.GetProjectionState<TestProjection>("non-existent");

        // Assert - no state should be created
        Assert.Null(state);
    }

    /// <summary>
    ///     Connection state should update when ProjectionConnectionChangedAction is dispatched.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void ConnectionStateUpdatesAfterConnectionChangedActionDispatched()
    {
        // Arrange
        ProjectionLoadedAction<TestProjection> loadedAction = new("entity-1", new("Test", 1), 1);
        ProjectionConnectionChangedAction<TestProjection> connectedAction = new("entity-1", true);

        // Act
        sut.Dispatch(loadedAction);
        sut.Dispatch(connectedAction);
        IProjectionState<TestProjection>? state = sut.GetProjectionState<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(state);
        Assert.True(state.IsConnected);
    }

    /// <summary>
    ///     Dispatch should throw ObjectDisposedException after disposal.
    /// </summary>
    [Fact]
    [AllureFeature("Disposal")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP016:Don't use disposed instance",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP017:Prefer using",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    public void DispatchAfterDisposeThrowsObjectDisposedException()
    {
        // Arrange - intentionally testing disposed object behavior
        RippleStore store = new();
        store.Dispose();
        ProjectionLoadingAction<TestProjection> action = new("entity-1");

        // Act & Assert - store was disposed
        Assert.Throws<ObjectDisposedException>(() => store.Dispatch(action));
    }

    /// <summary>
    ///     Dispatch should throw ArgumentNullException when action is null.
    /// </summary>
    [Fact]
    [AllureFeature("Input Validation")]
    public void DispatchWithNullActionThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => sut.Dispatch(null!));
        Assert.Equal("action", exception.ParamName);
    }

    /// <summary>
    ///     Dispose should not throw when called multiple times.
    /// </summary>
    [Fact]
    [AllureFeature("Disposal")]
    public void DisposeDoesNotThrowWhenCalledMultipleTimes()
    {
        // Arrange - create a separate store instance for this test
        using (RippleStore store = new())
        {
            // Act - call dispose explicitly, then let using call it again
            store.Dispose();
        }

        // Assert - if we get here, multiple dispose calls succeeded
        Assert.True(true);
    }

    /// <summary>
    ///     GetState should throw ObjectDisposedException after disposal.
    /// </summary>
    [Fact]
    [AllureFeature("Disposal")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP016:Don't use disposed instance",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP017:Prefer using",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    public void GetStateAfterDisposeThrowsObjectDisposedException()
    {
        // Arrange - intentionally testing disposed object behavior
        RippleStore store = new();
        store.Dispose();

        // Act & Assert - store was disposed
        Assert.Throws<ObjectDisposedException>(() => store.GetProjectionState<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     GetState should return null when no state has been dispatched for an entity.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void GetStateReturnsNullWhenNoStateExists()
    {
        // Arrange & Act
        IProjectionState<TestProjection>? state = sut.GetProjectionState<TestProjection>("entity-1");

        // Assert
        Assert.Null(state);
    }

    /// <summary>
    ///     GetState should return state after ProjectionLoadingAction is dispatched.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void GetStateReturnsStateAfterLoadingActionDispatched()
    {
        // Arrange
        ProjectionLoadingAction<TestProjection> action = new("entity-1");

        // Act
        sut.Dispatch(action);
        IProjectionState<TestProjection>? state = sut.GetProjectionState<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(state);
        Assert.Equal("entity-1", state.EntityId);
        Assert.True(state.IsLoading);
        Assert.False(state.IsLoaded);
    }

    /// <summary>
    ///     GetState should return state with data after ProjectionLoadedAction is dispatched.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void GetStateReturnsStateWithDataAfterLoadedActionDispatched()
    {
        // Arrange
        TestProjection data = new("Test", 42);
        ProjectionLoadedAction<TestProjection> loadedAction = new("entity-1", data, 1);

        // Act
        sut.Dispatch(loadedAction);
        IProjectionState<TestProjection>? state = sut.GetProjectionState<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(state);
        Assert.Equal("entity-1", state.EntityId);
        Assert.False(state.IsLoading);
        Assert.True(state.IsLoaded);
        Assert.Equal(data, state.Data);
        Assert.Equal(1, state.Version);
    }

    /// <summary>
    ///     GetState should return state with error after ProjectionErrorAction is dispatched.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void GetStateReturnsStateWithErrorAfterErrorActionDispatched()
    {
        // Arrange
        ProjectionLoadingAction<TestProjection> loadingAction = new("entity-1");
        InvalidOperationException error = new("Test error");
        ProjectionErrorAction<TestProjection> errorAction = new("entity-1", error);

        // Act
        sut.Dispatch(loadingAction);
        sut.Dispatch(errorAction);
        IProjectionState<TestProjection>? state = sut.GetProjectionState<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(state);
        Assert.Equal("entity-1", state.EntityId);
        Assert.False(state.IsLoading);
        Assert.False(state.IsLoaded);
        Assert.Same(error, state.LastError);
    }

    /// <summary>
    ///     Subscribe should throw ObjectDisposedException after disposal.
    /// </summary>
    [Fact]
    [AllureFeature("Disposal")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP016:Don't use disposed instance",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP017:Prefer using",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "Testing ObjectDisposedException behavior requires using disposed instance")]
    public void SubscribeAfterDisposeThrowsObjectDisposedException()
    {
        // Arrange - intentionally testing disposed object behavior
        RippleStore store = new();
        store.Dispose();

        // Act & Assert - store was disposed
        void ThrowingAction()
        {
            using IDisposable subscription = store.Subscribe(() => { });
        }

        Assert.Throws<ObjectDisposedException>(ThrowingAction);
    }

    /// <summary>
    ///     Subscribe should not invoke listener after disposal.
    /// </summary>
    [Fact]
    [AllureFeature("Subscription")]
    public void SubscribeDoesNotInvokeListenerAfterDisposal()
    {
        // Arrange
        int invokeCount = 0;
        using (IDisposable subscription = sut.Subscribe(() => invokeCount++))
        {
            ProjectionLoadingAction<TestProjection> action = new("entity-1");

            // Act
            sut.Dispatch(action);
        }

        // After using block, subscription is disposed
        sut.Dispatch(new ProjectionLoadingAction<TestProjection>("entity-2"));

        // Assert
        Assert.Equal(1, invokeCount);
    }

    /// <summary>
    ///     Subscribe should invoke listener when action is dispatched.
    /// </summary>
    [Fact]
    [AllureFeature("Subscription")]
    public void SubscribeInvokesListenerWhenActionDispatched()
    {
        // Arrange
        bool invoked = false;
        using IDisposable subscription = sut.Subscribe(() => invoked = true);
        ProjectionLoadingAction<TestProjection> action = new("entity-1");

        // Act
        sut.Dispatch(action);

        // Assert
        Assert.True(invoked);
    }

    /// <summary>
    ///     Subscribe should throw ArgumentNullException when listener is null.
    /// </summary>
    [Fact]
    [AllureFeature("Input Validation")]
    public void SubscribeWithNullListenerThrowsArgumentNullException()
    {
        // Arrange & Act & Assert - throws before returning IDisposable
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(ThrowingAction);
        Assert.Equal("listener", exception.ParamName);

        void ThrowingAction()
        {
            using IDisposable subscription = sut.Subscribe(null!);
        }
    }
}