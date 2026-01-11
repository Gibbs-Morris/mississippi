using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Abstractions.Actions;
using Mississippi.Inlet.Abstractions.State;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.L0Tests;

/// <summary>
///     Tests for <see cref="InletStore" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet")]
[AllureSuite("Core")]
[AllureSubSuite("InletStore")]
public sealed class InletStoreTests : IDisposable
{
    private readonly ServiceProvider serviceProvider;

    private readonly InletStore sut;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InletStoreTests" /> class.
    /// </summary>
    public InletStoreTests()
    {
        ServiceCollection services = [];
        serviceProvider = services.BuildServiceProvider();
        sut = new(serviceProvider);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        sut.Dispose();
        serviceProvider.Dispose();
    }

    /// <summary>
    ///     Test effect for CreateWithEffect tests.
    /// </summary>
    private sealed class TestEffect
        : IEffect,
          IDisposable
    {
        private readonly Action onAction;

        public TestEffect(
            Action onAction
        ) =>
            this.onAction = onAction;

        /// <inheritdoc />
        public bool CanHandle(
            IAction action
        ) =>
            true;

        /// <inheritdoc />
        public void Dispose()
        {
            // No-op
        }

        /// <inheritdoc />
#pragma warning disable CS1998 // Async method lacks 'await' operators
        public async IAsyncEnumerable<IAction> HandleAsync(
            IAction action,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
#pragma warning restore CS1998
        {
            onAction();
            yield break;
        }
    }

    /// <summary>
    ///     Test projection record for unit tests.
    /// </summary>
    /// <param name="Name">The projection name.</param>
    /// <param name="Value">The projection value.</param>
    private sealed record TestProjection(string Name, int Value = 0);

    /// <summary>
    ///     CreateWithEffect should create a new store with the effect registered.
    /// </summary>
    [Fact]
    [AllureFeature("Factory")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Store is disposed in this test")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Effect is owned and disposed by the store")]
    public void CreateWithEffectCreatesStoreWithEffect()
    {
        // Arrange
        bool effectCalled = false;

        // Act
        using InletStore store = InletStore.CreateWithEffect(s => new TestEffect(() => effectCalled = true));

        // Assert
        Assert.NotNull(store);

        // Dispatch to trigger effect
        store.Dispatch(new ProjectionLoadingAction<TestProjection>("entity-1"));
        Assert.True(effectCalled);
    }

    /// <summary>
    ///     CreateWithEffect should throw ArgumentNullException when factory is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Testing ArgumentNullException - no store is created")]
    public void CreateWithEffectWithNullFactoryThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => InletStore.CreateWithEffect<TestEffect>(null!));
    }

    /// <summary>
    ///     Dispose should throw ObjectDisposedException after disposal.
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
        // Arrange
        ServiceCollection services = [];
        using ServiceProvider testProvider = services.BuildServiceProvider();
        InletStore disposedStore = new(testProvider);
        disposedStore.Dispose();
        ProjectionLoadingAction<TestProjection> action = new("entity-1");

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => disposedStore.Dispatch(action));
    }

    /// <summary>
    ///     Dispatch ProjectionLoadedAction should update state.
    /// </summary>
    [Fact]
    [AllureFeature("Actions")]
    public void DispatchProjectionLoadedActionUpdatesState()
    {
        // Arrange
        TestProjection projection = new("Loaded", 100);
        ProjectionLoadedAction<TestProjection> action = new("entity-2", projection, 15L);

        // Act
        sut.Dispatch(action);
        TestProjection? result = sut.GetProjection<TestProjection>("entity-2");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Loaded", result.Name);
        Assert.Equal(100, result.Value);
    }

    /// <summary>
    ///     Dispatch ProjectionLoadingAction should set loading state.
    /// </summary>
    [Fact]
    [AllureFeature("Actions")]
    public void DispatchProjectionLoadingActionSetsLoadingState()
    {
        // Arrange
        ProjectionLoadingAction<TestProjection> action = new("entity-3");

        // Act
        sut.Dispatch(action);
        bool isLoading = sut.IsProjectionLoading<TestProjection>("entity-3");

        // Assert
        Assert.True(isLoading);
    }

    /// <summary>
    ///     GetProjectionError should return null for non-existent entity.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void GetProjectionErrorReturnsNullForNonExistentEntity()
    {
        // Act
        Exception? result = sut.GetProjectionError<TestProjection>("non-existent");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     GetProjection should return null for non-existent entity.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void GetProjectionReturnsNullForNonExistentEntity()
    {
        // Act
        TestProjection? result = sut.GetProjection<TestProjection>("non-existent");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     GetProjectionState should return null when entity does not exist.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void GetProjectionStateReturnsNullWhenNotExists()
    {
        // Act
        IProjectionState<TestProjection>? state = sut.GetProjectionState<TestProjection>("non-existent");

        // Assert
        Assert.Null(state);
    }

    /// <summary>
    ///     GetProjectionState should return state when exists.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void GetProjectionStateReturnsStateWhenExists()
    {
        // Arrange
        TestProjection projection = new("Test", 42);
        sut.NotifyProjectionUpdated("entity-1", projection, 5L);

        // Act
        IProjectionState<TestProjection>? state = sut.GetProjectionState<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(state);
        Assert.Equal(projection, state.Data);
        Assert.Equal(5L, state.Version);
    }

    /// <summary>
    ///     GetProjectionVersion should return -1 for non-existent entity.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void GetProjectionVersionReturnsNegativeOneForNonExistentEntity()
    {
        // Act
        long result = sut.GetProjectionVersion<TestProjection>("non-existent");

        // Assert
        Assert.Equal(-1, result);
    }

    /// <summary>
    ///     IsProjectionConnected should return false for non-existent entity.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void IsProjectionConnectedReturnsFalseForNonExistentEntity()
    {
        // Act
        bool result = sut.IsProjectionConnected<TestProjection>("non-existent");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    ///     IsProjectionLoading should return false for non-existent entity.
    /// </summary>
    [Fact]
    [AllureFeature("State Retrieval")]
    public void IsProjectionLoadingReturnsFalseForNonExistentEntity()
    {
        // Act
        bool result = sut.IsProjectionLoading<TestProjection>("non-existent");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    ///     NotifyConnectionChanged should update connection state.
    /// </summary>
    [Fact]
    [AllureFeature("Notifications")]
    public void NotifyConnectionChangedUpdatesConnectionState()
    {
        // Act
        sut.NotifyConnectionChanged<TestProjection>("entity-1", true);
        bool isConnected = sut.IsProjectionConnected<TestProjection>("entity-1");

        // Assert
        Assert.True(isConnected);
    }

    /// <summary>
    ///     NotifyConnectionChanged should update existing state.
    /// </summary>
    [Fact]
    [AllureFeature("Notifications")]
    public void NotifyConnectionChangedUpdatesExistingState()
    {
        // Arrange - first create a state
        TestProjection projection = new("Test", 42);
        sut.NotifyProjectionUpdated("entity-1", projection, 5L);

        // Act - then update connection
        sut.NotifyConnectionChanged<TestProjection>("entity-1", true);
        bool isConnected = sut.IsProjectionConnected<TestProjection>("entity-1");

        // Assert
        Assert.True(isConnected);

        // Original data should still be there
        TestProjection? result = sut.GetProjection<TestProjection>("entity-1");
        Assert.NotNull(result);
    }

    /// <summary>
    ///     NotifyError should store error.
    /// </summary>
    [Fact]
    [AllureFeature("Notifications")]
    public void NotifyErrorStoresError()
    {
        // Arrange
        InvalidOperationException error = new("Test error");

        // Act
        sut.NotifyError<TestProjection>("entity-1", error);
        Exception? result = sut.GetProjectionError<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test error", result.Message);
    }

    /// <summary>
    ///     NotifyError should update existing state.
    /// </summary>
    [Fact]
    [AllureFeature("Notifications")]
    public void NotifyErrorUpdatesExistingState()
    {
        // Arrange - first create a state
        TestProjection projection = new("Test", 42);
        sut.NotifyProjectionUpdated("entity-1", projection, 5L);
        InvalidOperationException error = new("Test error");

        // Act
        sut.NotifyError<TestProjection>("entity-1", error);
        Exception? result = sut.GetProjectionError<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test error", result.Message);
    }

    /// <summary>
    ///     NotifyProjectionUpdated should update version.
    /// </summary>
    [Fact]
    [AllureFeature("Notifications")]
    public void NotifyProjectionUpdatedSetsVersion()
    {
        // Arrange
        TestProjection projection = new("Test", 42);

        // Act
        sut.NotifyProjectionUpdated("entity-1", projection, 10L);
        long version = sut.GetProjectionVersion<TestProjection>("entity-1");

        // Assert
        Assert.Equal(10L, version);
    }

    /// <summary>
    ///     NotifyProjectionUpdated should store projection data.
    /// </summary>
    [Fact]
    [AllureFeature("Notifications")]
    public void NotifyProjectionUpdatedStoresProjectionData()
    {
        // Arrange
        TestProjection projection = new("Test", 42);

        // Act
        sut.NotifyProjectionUpdated("entity-1", projection, 5L);
        TestProjection? result = sut.GetProjection<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal(42, result.Value);
    }

    /// <summary>
    ///     NotifyProjectionUpdated should update existing state.
    /// </summary>
    [Fact]
    [AllureFeature("Notifications")]
    public void NotifyProjectionUpdatedUpdatesExistingState()
    {
        // Arrange - first create a state
        TestProjection projection1 = new("First", 1);
        sut.NotifyProjectionUpdated("entity-1", projection1, 1L);

        // Act - update with new data
        TestProjection projection2 = new("Second", 2);
        sut.NotifyProjectionUpdated("entity-1", projection2, 2L);

        // Assert
        TestProjection? result = sut.GetProjection<TestProjection>("entity-1");
        Assert.NotNull(result);
        Assert.Equal("Second", result.Name);
        Assert.Equal(2, result.Value);
        Assert.Equal(2L, sut.GetProjectionVersion<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     Parameterless constructor should create valid store.
    /// </summary>
    [Fact]
    [AllureFeature("Construction")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Store is disposed in this test")]
    public void ParameterlessConstructorCreatesValidStore()
    {
        // Act
        using InletStore store = new();

        // Assert
        Assert.NotNull(store);

        // Should be able to dispatch without throwing
        store.Dispatch(new ProjectionLoadingAction<TestProjection>("entity-1"));
        bool isLoading = store.IsProjectionLoading<TestProjection>("entity-1");
        Assert.True(isLoading);
    }
}