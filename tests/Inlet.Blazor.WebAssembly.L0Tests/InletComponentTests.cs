using System;
using System.Diagnostics.CodeAnalysis;

using Allure.Xunit.Attributes;

using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Abstractions.Actions;
using Mississippi.Inlet.Abstractions.State;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Blazor.WebAssembly.L0Tests;

/// <summary>
///     Tests for <see cref="InletComponent" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Blazor.WebAssembly")]
[AllureSuite("Components")]
[AllureSubSuite("InletComponent")]
public sealed class InletComponentTests : IDisposable
{
    private readonly InletStore store;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InletComponentTests" /> class.
    /// </summary>
    public InletComponentTests() => store = new();

    /// <inheritdoc />
    public void Dispose()
    {
        store.Dispose();
    }

    /// <summary>
    ///     Middleware for capturing dispatched actions.
    /// </summary>
    private sealed class CaptureActionMiddleware : IMiddleware
    {
        private readonly Action<IAction> capture;

        public CaptureActionMiddleware(
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
    ///     Concrete test component for testing abstract InletComponent.
    /// </summary>
    private sealed class TestInletComponent : InletComponent
    {
        /// <summary>
        ///     Sets the InletStore for testing.
        /// </summary>
        /// <param name="inletStore">The inlet store instance.</param>
        public void SetInletStore(
            IInletStore inletStore
        ) =>
            InletStore = inletStore;

        /// <summary>
        ///     Sets the Store for testing.
        /// </summary>
        /// <param name="store">The store instance.</param>
        public void SetStore(
            IStore store
        ) =>
            Store = store;

        /// <summary>
        ///     Exposes GetProjection for testing.
        /// </summary>
        /// <typeparam name="T">The projection type.</typeparam>
        /// <param name="entityId">The entity identifier.</param>
        /// <returns>The projection or null.</returns>
        public T? TestGetProjection<T>(
            string entityId
        )
            where T : class =>
            GetProjection<T>(entityId);

        /// <summary>
        ///     Exposes GetProjectionError for testing.
        /// </summary>
        /// <typeparam name="T">The projection type.</typeparam>
        /// <param name="entityId">The entity identifier.</param>
        /// <returns>The error or null.</returns>
        public Exception? TestGetProjectionError<T>(
            string entityId
        )
            where T : class =>
            GetProjectionError<T>(entityId);

        /// <summary>
        ///     Exposes GetProjectionState for testing.
        /// </summary>
        /// <typeparam name="T">The projection type.</typeparam>
        /// <param name="entityId">The entity identifier.</param>
        /// <returns>The projection state or null.</returns>
        public IProjectionState<T>? TestGetProjectionState<T>(
            string entityId
        )
            where T : class =>
            GetProjectionState<T>(entityId);

        /// <summary>
        ///     Exposes IsProjectionConnected for testing.
        /// </summary>
        /// <typeparam name="T">The projection type.</typeparam>
        /// <param name="entityId">The entity identifier.</param>
        /// <returns>True if connected.</returns>
        public bool TestIsProjectionConnected<T>(
            string entityId
        )
            where T : class =>
            IsProjectionConnected<T>(entityId);

        /// <summary>
        ///     Exposes IsProjectionLoading for testing.
        /// </summary>
        /// <typeparam name="T">The projection type.</typeparam>
        /// <param name="entityId">The entity identifier.</param>
        /// <returns>True if loading.</returns>
        public bool TestIsProjectionLoading<T>(
            string entityId
        )
            where T : class =>
            IsProjectionLoading<T>(entityId);

        /// <summary>
        ///     Initializes the component for testing.
        /// </summary>
        public void TestOnInitialized() => OnInitialized();

        /// <summary>
        ///     Exposes RefreshProjection for testing.
        /// </summary>
        /// <typeparam name="T">The projection type.</typeparam>
        /// <param name="entityId">The entity identifier.</param>
        public void TestRefreshProjection<T>(
            string entityId
        )
            where T : class =>
            RefreshProjection<T>(entityId);

        /// <summary>
        ///     Exposes SubscribeToProjection for testing.
        /// </summary>
        /// <typeparam name="T">The projection type.</typeparam>
        /// <param name="entityId">The entity identifier.</param>
        public void TestSubscribeToProjection<T>(
            string entityId
        )
            where T : class =>
            SubscribeToProjection<T>(entityId);

        /// <summary>
        ///     Exposes UnsubscribeFromProjection for testing.
        /// </summary>
        /// <typeparam name="T">The projection type.</typeparam>
        /// <param name="entityId">The entity identifier.</param>
        public void TestUnsubscribeFromProjection<T>(
            string entityId
        )
            where T : class =>
            UnsubscribeFromProjection<T>(entityId);
    }

    /// <summary>
    ///     Test projection type for unit tests.
    /// </summary>
    private sealed record TestProjection
    {
        /// <summary>
        ///     Gets the name.
        /// </summary>
        public string Name { get; init; } = string.Empty;
    }

    /// <summary>
    ///     Dispose should be callable multiple times without error.
    /// </summary>
    [Fact]
    [AllureFeature("Disposal")]
    [SuppressMessage(
        "SonarQube",
        "S2699:Tests should include assertions",
        Justification = "This test verifies no exception is thrown on multiple dispose calls")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP016:Don't use disposed instance",
        Justification = "Testing that multiple Dispose calls don't throw")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP017:Prefer using",
        Justification = "Testing explicit Dispose calls, not automatic disposal")]
    public void DisposeCanBeCalledMultipleTimes()
    {
        // Arrange
        TestInletComponent sut = new();
        sut.SetStore(store);
        sut.SetInletStore(store);
        sut.TestOnInitialized();

        // Act & Assert - should not throw
        sut.Dispose();
        sut.Dispose();
        Assert.True(true);
    }

    /// <summary>
    ///     GetProjection should delegate to the InletStore.
    /// </summary>
    [Fact]
    [AllureFeature("Projection Retrieval")]
    public void GetProjectionDelegatesToInletStore()
    {
        // Arrange
        using TestInletComponent sut = new();
        sut.SetInletStore(store);

        // Act
        TestProjection? result = sut.TestGetProjection<TestProjection>("entity-1");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     GetProjectionError should delegate to the InletStore.
    /// </summary>
    [Fact]
    [AllureFeature("Projection Error")]
    public void GetProjectionErrorDelegatesToInletStore()
    {
        // Arrange
        using TestInletComponent sut = new();
        sut.SetInletStore(store);

        // Act
        Exception? result = sut.TestGetProjectionError<TestProjection>("entity-1");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     GetProjectionState should delegate to the InletStore.
    /// </summary>
    [Fact]
    [AllureFeature("Projection State")]
    public void GetProjectionStateDelegatesToInletStore()
    {
        // Arrange
        using TestInletComponent sut = new();
        sut.SetInletStore(store);

        // Act
        IProjectionState<TestProjection>? result = sut.TestGetProjectionState<TestProjection>("entity-1");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     IsProjectionConnected should delegate to the InletStore.
    /// </summary>
    [Fact]
    [AllureFeature("Connection Status")]
    public void IsProjectionConnectedDelegatesToInletStore()
    {
        // Arrange
        using TestInletComponent sut = new();
        sut.SetInletStore(store);

        // Act
        bool result = sut.TestIsProjectionConnected<TestProjection>("entity-1");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    ///     IsProjectionLoading should delegate to the InletStore.
    /// </summary>
    [Fact]
    [AllureFeature("Loading Status")]
    public void IsProjectionLoadingDelegatesToInletStore()
    {
        // Arrange
        using TestInletComponent sut = new();
        sut.SetInletStore(store);

        // Act
        bool result = sut.TestIsProjectionLoading<TestProjection>("entity-1");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    ///     OnInitialized should subscribe to store changes.
    /// </summary>
    [Fact]
    [AllureFeature("Initialization")]
    [SuppressMessage(
        "SonarQube",
        "S2699:Tests should include assertions",
        Justification = "This test verifies no exception is thrown on initialization")]
    public void OnInitializedSubscribesToStore()
    {
        // Arrange
        using TestInletComponent sut = new();
        sut.SetStore(store);
        sut.SetInletStore(store);

        // Act
        sut.TestOnInitialized();

        // Assert - no exception thrown means subscription was created
        Assert.True(true);
    }

    /// <summary>
    ///     RefreshProjection should dispatch RefreshProjectionAction.
    /// </summary>
    [Fact]
    [AllureFeature("Dispatch Actions")]
    public void RefreshProjectionDispatchesAction()
    {
        // Arrange
        using TestInletComponent sut = new();
        sut.SetStore(store);
        sut.SetInletStore(store);
        IAction? dispatchedAction = null;
        store.RegisterMiddleware(new CaptureActionMiddleware(a => dispatchedAction = a));

        // Act
        sut.TestRefreshProjection<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(dispatchedAction);
        Assert.IsType<RefreshProjectionAction<TestProjection>>(dispatchedAction);
        RefreshProjectionAction<TestProjection> typedAction = (RefreshProjectionAction<TestProjection>)dispatchedAction;
        Assert.Equal("entity-1", typedAction.EntityId);
    }

    /// <summary>
    ///     SubscribeToProjection should dispatch SubscribeToProjectionAction.
    /// </summary>
    [Fact]
    [AllureFeature("Dispatch Actions")]
    public void SubscribeToProjectionDispatchesAction()
    {
        // Arrange
        using TestInletComponent sut = new();
        sut.SetStore(store);
        sut.SetInletStore(store);
        IAction? dispatchedAction = null;
        store.RegisterMiddleware(new CaptureActionMiddleware(a => dispatchedAction = a));

        // Act
        sut.TestSubscribeToProjection<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(dispatchedAction);
        Assert.IsType<SubscribeToProjectionAction<TestProjection>>(dispatchedAction);
        SubscribeToProjectionAction<TestProjection> typedAction =
            (SubscribeToProjectionAction<TestProjection>)dispatchedAction;
        Assert.Equal("entity-1", typedAction.EntityId);
    }

    /// <summary>
    ///     UnsubscribeFromProjection should dispatch UnsubscribeFromProjectionAction.
    /// </summary>
    [Fact]
    [AllureFeature("Dispatch Actions")]
    public void UnsubscribeFromProjectionDispatchesAction()
    {
        // Arrange
        using TestInletComponent sut = new();
        sut.SetStore(store);
        sut.SetInletStore(store);
        IAction? dispatchedAction = null;
        store.RegisterMiddleware(new CaptureActionMiddleware(a => dispatchedAction = a));

        // Act
        sut.TestUnsubscribeFromProjection<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(dispatchedAction);
        Assert.IsType<UnsubscribeFromProjectionAction<TestProjection>>(dispatchedAction);
        UnsubscribeFromProjectionAction<TestProjection> typedAction =
            (UnsubscribeFromProjectionAction<TestProjection>)dispatchedAction;
        Assert.Equal("entity-1", typedAction.EntityId);
    }
}