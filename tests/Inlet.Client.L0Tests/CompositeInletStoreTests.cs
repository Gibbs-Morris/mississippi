using System;
using System.Diagnostics.CodeAnalysis;

using Allure.Xunit.Attributes;

using Mississippi.Inlet.Abstractions.State;
using Mississippi.Reservoir;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Client.L0Tests;

/// <summary>
///     Tests for <see cref="CompositeInletStore" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet")]
[AllureSuite("Core")]
[AllureSubSuite("CompositeInletStore")]
public sealed class CompositeInletStoreTests : IDisposable
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
    [AllureFeature("Validation")]
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
    [AllureFeature("Validation")]
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
    [AllureFeature("Delegation")]
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
    [AllureFeature("Disposal")]
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
        // Arrange
        Store localStore = new();
        CompositeInletStore sut = new(localStore, cache);

        // Act - call dispose twice (which disposes localStore)
        sut.Dispose();
        sut.Dispose();

        // Assert - no exception thrown
    }

    /// <summary>
    ///     GetProjection should delegate to projection cache.
    /// </summary>
    [Fact]
    [AllureFeature("Delegation")]
    public void GetProjectionDelegatesToCache()
    {
        // Arrange
        TestProjection data = new("Test");
        cache.SetLoaded("entity-1", data, 5L);
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
    [AllureFeature("Delegation")]
    public void GetProjectionErrorDelegatesToCache()
    {
        // Arrange
        InvalidOperationException error = new("Test error");
        cache.SetError<TestProjection>("entity-1", error);
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
    [AllureFeature("Delegation")]
    public void GetProjectionStateDelegatesToCache()
    {
        // Arrange
        TestProjection data = new("Test");
        cache.SetLoaded("entity-1", data, 7L);
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
    [AllureFeature("Delegation")]
    public void GetProjectionVersionDelegatesToCache()
    {
        // Arrange
        TestProjection data = new("Test");
        cache.SetLoaded("entity-1", data, 42L);
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
    [AllureFeature("Delegation")]
    public void IsProjectionConnectedDelegatesToCache()
    {
        // Arrange
        cache.SetConnection<TestProjection>("entity-1", true);
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
    [AllureFeature("Delegation")]
    public void IsProjectionLoadingDelegatesToCache()
    {
        // Arrange
        cache.SetLoading<TestProjection>("entity-1");
        using CompositeInletStore sut = new(store, cache);

        // Act
        bool result = sut.IsProjectionLoading<TestProjection>("entity-1");

        // Assert
        Assert.True(result);
    }

    /// <summary>
    ///     SetConnection should delegate to projection cache.
    /// </summary>
    [Fact]
    [AllureFeature("Delegation")]
    public void SetConnectionDelegatesToCache()
    {
        // Arrange
        using CompositeInletStore sut = new(store, cache);

        // Act
        sut.SetConnection<TestProjection>("entity-1", true);

        // Assert
        Assert.True(cache.IsProjectionConnected<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     SetError should delegate to projection cache.
    /// </summary>
    [Fact]
    [AllureFeature("Delegation")]
    public void SetErrorDelegatesToCache()
    {
        // Arrange
        using CompositeInletStore sut = new(store, cache);
        InvalidOperationException error = new("Test error");

        // Act
        sut.SetError<TestProjection>("entity-1", error);

        // Assert
        Exception? result = cache.GetProjectionError<TestProjection>("entity-1");
        Assert.NotNull(result);
        Assert.Equal("Test error", result.Message);
    }

    /// <summary>
    ///     SetLoaded should delegate to projection cache.
    /// </summary>
    [Fact]
    [AllureFeature("Delegation")]
    public void SetLoadedDelegatesToCache()
    {
        // Arrange
        using CompositeInletStore sut = new(store, cache);
        TestProjection data = new("Test");

        // Act
        sut.SetLoaded("entity-1", data, 10L);

        // Assert
        TestProjection? result = cache.GetProjection<TestProjection>("entity-1");
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal(10L, cache.GetProjectionVersion<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     SetLoading should delegate to projection cache.
    /// </summary>
    [Fact]
    [AllureFeature("Delegation")]
    public void SetLoadingDelegatesToCache()
    {
        // Arrange
        using CompositeInletStore sut = new(store, cache);

        // Act
        sut.SetLoading<TestProjection>("entity-1");

        // Assert
        Assert.True(cache.IsProjectionLoading<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     SetUpdated should delegate to projection cache.
    /// </summary>
    [Fact]
    [AllureFeature("Delegation")]
    public void SetUpdatedDelegatesToCache()
    {
        // Arrange
        using CompositeInletStore sut = new(store, cache);
        TestProjection data = new("Updated");

        // Act
        sut.SetUpdated("entity-1", data, 15L);

        // Assert
        TestProjection? result = cache.GetProjection<TestProjection>("entity-1");
        Assert.NotNull(result);
        Assert.Equal("Updated", result.Name);
        Assert.Equal(15L, cache.GetProjectionVersion<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     Subscribe should delegate to underlying store.
    /// </summary>
    [Fact]
    [AllureFeature("Delegation")]
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