using System;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Inlet.Client.Reducers;
using Mississippi.Reservoir;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Client.L0Tests;

/// <summary>
///     Tests for <see cref="CompositeInletStore" />.
/// </summary>
public sealed class CompositeInletStoreTests : IDisposable
{
    private readonly ProjectionCache cache;

    private readonly ServiceProvider serviceProvider;

    private readonly Store store;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CompositeInletStoreTests" /> class.
    /// </summary>
    public CompositeInletStoreTests()
    {
        // Set up DI container with Store and ProjectionsFeatureState
        ServiceCollection services = new();
        services.AddReservoir();
        services.AddFeatureState<ProjectionsFeatureState>();
        services.AddReducer<ProjectionLoadingAction<TestProjection>, ProjectionsFeatureState>(
            ProjectionsReducer.ReduceLoading);
        services.AddReducer<ProjectionLoadedAction<TestProjection>, ProjectionsFeatureState>(
            ProjectionsReducer.ReduceLoaded);
        services.AddReducer<ProjectionUpdatedAction<TestProjection>, ProjectionsFeatureState>(
            ProjectionsReducer.ReduceUpdated);
        services.AddReducer<ProjectionErrorAction<TestProjection>, ProjectionsFeatureState>(
            ProjectionsReducer.ReduceError);
        services.AddReducer<ProjectionConnectionChangedAction<TestProjection>, ProjectionsFeatureState>(
            ProjectionsReducer.ReduceConnectionChanged);
        serviceProvider = services.BuildServiceProvider();
        store = (Store)serviceProvider.GetRequiredService<IStore>();
        cache = new(store);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        store.Dispose();
        serviceProvider.Dispose();
    }

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
    ///     Test action for dispatch tests.
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
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Assert.Throws catches the exception before any object is created")]
    public void ConstructorThrowsWhenProjectionCacheIsNull() =>
        Assert.Throws<ArgumentNullException>(() => new CompositeInletStore(store, null!));

    /// <summary>
    ///     Constructor should throw when store is null.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Assert.Throws catches the exception before any object is created")]
    public void ConstructorThrowsWhenStoreIsNull() =>
        Assert.Throws<ArgumentNullException>(() => new CompositeInletStore(null!, cache));

    /// <summary>
    ///     Dispatch should delegate to underlying store.
    /// </summary>
    [Fact]
    public void DispatchDelegatesToStore()
    {
        // Arrange
        IAction? capturedAction = null;
        store.RegisterMiddleware(new CaptureMiddleware(a => capturedAction = a));
        using CompositeInletStore sut = new(store, cache);
        TestAction action = new();

        // Act
        sut.Dispatch(action);

        // Assert
        Assert.Same(action, capturedAction);
    }

    /// <summary>
    ///     Dispose should be callable multiple times without error.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "SonarQube",
        "S2699:Tests should include assertions",
        Justification = "This test verifies no exception is thrown on multiple dispose calls")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP016:Don't use disposed instance",
        Justification = "Intentionally testing multiple dispose calls")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP017:Prefer using",
        Justification = "Testing explicit dispose pattern")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Testing explicit dispose pattern - resources are disposed in the test")]
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "Objects are explicitly disposed as part of the test")]
    public void DisposeCanBeCalledMultipleTimes()
    {
        // Arrange - create a new isolated store/cache pair for this test
        using ServiceProvider localServiceProvider = new ServiceCollection().AddReservoir()
            .AddFeatureState<ProjectionsFeatureState>()
            .BuildServiceProvider();
        Store localStore = (Store)localServiceProvider.GetRequiredService<IStore>();
        ProjectionCache localCache = new(localStore);
        CompositeInletStore sut = new(localStore, localCache);

        // Act - call dispose twice
        sut.Dispose();
        sut.Dispose();

        // Assert - no exception thrown
    }

    /// <summary>
    ///     GetProjection should delegate to projection cache.
    /// </summary>
    [Fact]
    public void GetProjectionDelegatesToCache()
    {
        // Arrange
        TestProjection data = new("Test");
        store.Dispatch(new ProjectionLoadedAction<TestProjection>("entity-1", data, 5L));
        using CompositeInletStore sut = new(store, cache);

        // Act
        TestProjection? result = sut.GetProjection<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
    }

    /// <summary>
    ///     GetProjectionError should delegate to projection cache.
    /// </summary>
    [Fact]
    public void GetProjectionErrorDelegatesToCache()
    {
        // Arrange
        InvalidOperationException error = new("Test error");
        store.Dispatch(new ProjectionErrorAction<TestProjection>("entity-1", error));
        using CompositeInletStore sut = new(store, cache);

        // Act
        Exception? result = sut.GetProjectionError<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test error", result.Message);
    }

    /// <summary>
    ///     GetProjectionState should delegate to projection cache.
    /// </summary>
    [Fact]
    public void GetProjectionStateDelegatesToCache()
    {
        // Arrange
        TestProjection data = new("Test");
        store.Dispatch(new ProjectionLoadedAction<TestProjection>("entity-1", data, 7L));
        using CompositeInletStore sut = new(store, cache);

        // Act
        IProjectionState<TestProjection>? state = sut.GetProjectionState<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(state);
        Assert.NotNull(state.Data);
        Assert.Equal("Test", state.Data.Name);
        Assert.Equal(7L, state.Version);
    }

    /// <summary>
    ///     GetProjectionVersion should delegate to projection cache.
    /// </summary>
    [Fact]
    public void GetProjectionVersionDelegatesToCache()
    {
        // Arrange
        TestProjection data = new("Test");
        store.Dispatch(new ProjectionLoadedAction<TestProjection>("entity-1", data, 42L));
        using CompositeInletStore sut = new(store, cache);

        // Act
        long version = sut.GetProjectionVersion<TestProjection>("entity-1");

        // Assert
        Assert.Equal(42L, version);
    }

    /// <summary>
    ///     IsProjectionConnected should delegate to projection cache.
    /// </summary>
    [Fact]
    public void IsProjectionConnectedDelegatesToCache()
    {
        // Arrange
        store.Dispatch(new ProjectionConnectionChangedAction<TestProjection>("entity-1", true));
        using CompositeInletStore sut = new(store, cache);

        // Act
        bool result = sut.IsProjectionConnected<TestProjection>("entity-1");

        // Assert
        Assert.True(result);
    }

    /// <summary>
    ///     IsProjectionLoading should delegate to projection cache.
    /// </summary>
    [Fact]
    public void IsProjectionLoadingDelegatesToCache()
    {
        // Arrange
        store.Dispatch(new ProjectionLoadingAction<TestProjection>("entity-1"));
        using CompositeInletStore sut = new(store, cache);

        // Act
        bool result = sut.IsProjectionLoading<TestProjection>("entity-1");

        // Assert
        Assert.True(result);
    }

    /// <summary>
    ///     Subscribe should delegate to underlying store.
    /// </summary>
    [Fact]
    public void SubscribeDelegatesToStore()
    {
        // Arrange
        using CompositeInletStore sut = new(store, cache);
        int callCount = 0;

        // Act
        using IDisposable subscription = sut.Subscribe(() => callCount++);
        sut.Dispatch(new TestAction());

        // Assert
        Assert.Equal(1, callCount);
    }
}