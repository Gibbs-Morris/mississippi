using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.AspNetCore.SignalR.Client;

using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Abstractions.Actions;
using Mississippi.Inlet.Blazor.WebAssembly.Effects;
using Mississippi.Inlet.Projection.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;

using Moq;


namespace Mississippi.Inlet.Blazor.WebAssembly.L0Tests.Effects;

/// <summary>
///     Tests for <see cref="InletSignalREffect" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Blazor.WebAssembly")]
[AllureSuite("Effects")]
[AllureSubSuite("InletSignalREffect")]
public sealed class InletSignalREffectTests : IAsyncDisposable
{
    private const string TestEntityId = "test-entity-123";

    private const string TestProjectionPath = "test-projections";

    private readonly InletSignalREffect? effect;

    private readonly Mock<IProjectionFetcher> fetcherMock = new();

    private readonly Mock<IHubConnectionProvider> hubProviderMock = new();

    private readonly Lazy<IInletStore> lazyStore;

    private readonly ProjectionDtoRegistry registry = new();

    private readonly Mock<IInletStore> storeMock = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="InletSignalREffectTests" /> class.
    /// </summary>
    public InletSignalREffectTests()
    {
        // Register test projection in registry
        registry.Register(TestProjectionPath, typeof(TestProjection));

        // Set up lazy store
        lazyStore = new Lazy<IInletStore>(() => storeMock.Object);

        // Set up hub provider mock
        hubProviderMock
            .Setup(h => h.RegisterHandler<string, string, long>(
                It.IsAny<string>(),
                It.IsAny<Func<string, string, long, Task>>()))
            .Returns(Mock.Of<IDisposable>());

        // Create effect
        effect = new(lazyStore, hubProviderMock.Object, fetcherMock.Object, registry);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (effect is not null)
        {
            await effect.DisposeAsync();
        }
    }

    /// <summary>
    ///     Test projection DTO for testing.
    /// </summary>
    [ProjectionPath(TestProjectionPath)]
    internal sealed record TestProjection(string Name, int Value);

    /// <summary>
    ///     Non-projection action for negative testing.
    /// </summary>
    private sealed record NonProjectionAction : IAction;

    /// <summary>
    ///     Verifies that CanHandle returns false for ProjectionErrorAction (output only).
    /// </summary>
    [Fact]
    [AllureFeature("CanHandle")]
    public void CanHandleReturnsFalseForErrorAction()
    {
        // Arrange - Error action is an output action, not input
        ProjectionErrorAction<TestProjection> action = new(TestEntityId, new InvalidOperationException("test"));

        // Act
        bool result = effect!.CanHandle(action);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    ///     Verifies that CanHandle returns false for ProjectionLoadingAction (output only).
    /// </summary>
    [Fact]
    [AllureFeature("CanHandle")]
    public void CanHandleReturnsFalseForLoadingAction()
    {
        // Arrange - Loading action is an output action, not input
        ProjectionLoadingAction<TestProjection> action = new(TestEntityId);

        // Act
        bool result = effect!.CanHandle(action);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    ///     Verifies that CanHandle returns false for non-projection actions.
    /// </summary>
    [Fact]
    [AllureFeature("CanHandle")]
    public void CanHandleReturnsFalseForNonProjectionAction()
    {
        // Arrange
        IAction action = new NonProjectionAction();

        // Act
        bool result = effect!.CanHandle(action);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    ///     Verifies that CanHandle returns false for generic action that is not a projection action.
    /// </summary>
    [Fact]
    [AllureFeature("CanHandle")]
    public void CanHandleReturnsFalseForNonProjectionGenericAction()
    {
        // Arrange - ProjectionLoadedAction is a result action, not handled by effect
        ProjectionLoadedAction<TestProjection> action = new(TestEntityId, new("test", 1), 1L);

        // Act
        bool result = effect!.CanHandle(action);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    ///     Verifies that CanHandle returns false for ProjectionUpdatedAction (output only).
    /// </summary>
    [Fact]
    [AllureFeature("CanHandle")]
    public void CanHandleReturnsFalseForUpdatedAction()
    {
        // Arrange - Updated action is an output action, not input
        ProjectionUpdatedAction<TestProjection> action = new(TestEntityId, new("test", 1), 1L);

        // Act
        bool result = effect!.CanHandle(action);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    ///     Verifies that CanHandle returns true for RefreshProjectionAction.
    /// </summary>
    [Fact]
    [AllureFeature("CanHandle")]
    public void CanHandleReturnsTrueForRefreshAction()
    {
        // Arrange
        RefreshProjectionAction<TestProjection> action = new(TestEntityId);

        // Act
        bool result = effect!.CanHandle(action);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    ///     Verifies that CanHandle returns true for SubscribeToProjectionAction.
    /// </summary>
    [Fact]
    [AllureFeature("CanHandle")]
    public void CanHandleReturnsTrueForSubscribeAction()
    {
        // Arrange
        SubscribeToProjectionAction<TestProjection> action = new(TestEntityId);

        // Act
        bool result = effect!.CanHandle(action);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    ///     Verifies that CanHandle returns true for UnsubscribeFromProjectionAction.
    /// </summary>
    [Fact]
    [AllureFeature("CanHandle")]
    public void CanHandleReturnsTrueForUnsubscribeAction()
    {
        // Arrange
        UnsubscribeFromProjectionAction<TestProjection> action = new(TestEntityId);

        // Act
        bool result = effect!.CanHandle(action);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    ///     Verifies that CanHandle throws when action is null.
    /// </summary>
    [Fact]
    [AllureFeature("CanHandle")]
    public void CanHandleThrowsWhenActionIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => effect!.CanHandle(null!));
    }

    /// <summary>
    ///     Verifies that constructor registers handler for projection updates.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Constructor")]
    public async Task ConstructorRegistersProjectionUpdateHandler()
    {
        // Arrange
        Mock<IHubConnectionProvider> localHubProviderMock = new();
        localHubProviderMock.Setup(h => h.RegisterHandler<string, string, long>(
                InletHubConstants.ProjectionUpdatedMethod,
                It.IsAny<Func<string, string, long, Task>>()))
            .Returns(Mock.Of<IDisposable>());
        InletSignalREffect localEffect = new(
            lazyStore,
            localHubProviderMock.Object,
            fetcherMock.Object,
            registry);

        // Act & Assert
        localHubProviderMock.Verify(
            h => h.RegisterHandler<string, string, long>(
                InletHubConstants.ProjectionUpdatedMethod,
                It.IsAny<Func<string, string, long, Task>>()),
            Times.Once);
        await localEffect.DisposeAsync();
    }

    /// <summary>
    ///     Verifies that constructor registers reconnection handler.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Constructor")]
    public async Task ConstructorRegistersReconnectionHandler()
    {
        // Arrange
        Mock<IHubConnectionProvider> localHubProviderMock = new();
        localHubProviderMock
            .Setup(h => h.RegisterHandler<string, string, long>(
                It.IsAny<string>(),
                It.IsAny<Func<string, string, long, Task>>()))
            .Returns(Mock.Of<IDisposable>());
        InletSignalREffect localEffect = new(
            lazyStore,
            localHubProviderMock.Object,
            fetcherMock.Object,
            registry);

        // Act & Assert
        localHubProviderMock.Verify(h => h.OnReconnected(It.IsAny<Func<string?, Task>>()), Times.Once);
        await localEffect.DisposeAsync();
    }

    /// <summary>
    ///     Verifies that constructor throws when hub connection provider is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenHubConnectionProviderIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new InletSignalREffect(
            lazyStore,
            null!,
            fetcherMock.Object,
            registry));
    }

    /// <summary>
    ///     Verifies that constructor throws when projection DTO registry is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenProjectionDtoRegistryIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new InletSignalREffect(
            lazyStore,
            hubProviderMock.Object,
            fetcherMock.Object,
            null!));
    }

    /// <summary>
    ///     Verifies that constructor throws when projection fetcher is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenProjectionFetcherIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new InletSignalREffect(
            lazyStore,
            hubProviderMock.Object,
            null!,
            registry));
    }

    /// <summary>
    ///     Verifies that constructor throws when lazy store is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenLazyStoreIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new InletSignalREffect(
            null!,
            hubProviderMock.Object,
            fetcherMock.Object,
            registry));
    }

    /// <summary>
    ///     Verifies that DisposeAsync can be called safely.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("DisposeAsync")]
    public async Task DisposeAsyncCleansUpResources()
    {
        // Arrange
        Mock<IHubConnectionProvider> localHubProviderMock = new();
        Mock<IDisposable> callbackDisposableMock = new();
        localHubProviderMock
            .Setup(h => h.RegisterHandler<string, string, long>(
                It.IsAny<string>(),
                It.IsAny<Func<string, string, long, Task>>()))
            .Returns(callbackDisposableMock.Object);
        InletSignalREffect localEffect = new(
            lazyStore,
            localHubProviderMock.Object,
            fetcherMock.Object,
            registry);

        // Act
        await localEffect.DisposeAsync();

        // Assert - callback registration was disposed
        callbackDisposableMock.Verify(d => d.Dispose(), Times.Once);
    }

    /// <summary>
    ///     Verifies that HandleAsync throws when action is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("HandleAsync")]
    public async Task HandleAsyncThrowsWhenActionIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await foreach (IAction action in effect!.HandleAsync(null!, CancellationToken.None))
            {
                // Should not execute
            }
        });
    }

    /// <summary>
    ///     Verifies that HandleAsync calls EnsureConnectedAsync on the hub provider.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("HandleAsync")]
    public async Task HandleAsyncEnsuresConnectionBeforeHandling()
    {
        // Arrange - set up EnsureConnectedAsync to succeed
        hubProviderMock.Setup(h => h.EnsureConnectedAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Set up the fetcher to return a result (for refresh action to succeed)
        TestProjection projection = new("test", 42);
        fetcherMock
            .Setup(f => f.FetchAsync(typeof(TestProjection), TestEntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectionFetchResult(projection, 1L));

        RefreshProjectionAction<TestProjection> action = new(TestEntityId);

        // Act - enumerate to trigger execution
        List<IAction> results = [];
        await foreach (IAction resultAction in effect!.HandleAsync(action, CancellationToken.None))
        {
            results.Add(resultAction);
        }

        // Assert
        hubProviderMock.Verify(h => h.EnsureConnectedAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     Verifies that OnProjectionUpdatedAsync ignores updates for unregistered paths.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("OnProjectionUpdated")]
    public async Task OnProjectionUpdatedIgnoresUnregisteredPath()
    {
        // Arrange - capture the callback
        Func<string, string, long, Task>? capturedCallback = null;
        Mock<IHubConnectionProvider> localHubProviderMock = new();
        localHubProviderMock
            .Setup(h => h.RegisterHandler<string, string, long>(
                InletHubConstants.ProjectionUpdatedMethod,
                It.IsAny<Func<string, string, long, Task>>()))
            .Callback<string, Func<string, string, long, Task>>((_, cb) => capturedCallback = cb)
            .Returns(Mock.Of<IDisposable>());

        InletSignalREffect localEffect = new(
            lazyStore,
            localHubProviderMock.Object,
            fetcherMock.Object,
            registry);

        // Act - invoke with unregistered path
        Assert.NotNull(capturedCallback);
        await capturedCallback("unregistered-path", TestEntityId, 1L);

        // Assert - store should not be called since path is not registered
        storeMock.Verify(s => s.Dispatch(It.IsAny<IAction>()), Times.Never);

        await localEffect.DisposeAsync();
    }

    /// <summary>
    ///     Verifies that OnProjectionUpdatedAsync ignores updates for non-subscribed projections.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("OnProjectionUpdated")]
    public async Task OnProjectionUpdatedIgnoresNonSubscribedProjection()
    {
        // Arrange - capture the callback
        Func<string, string, long, Task>? capturedCallback = null;
        Mock<IHubConnectionProvider> localHubProviderMock = new();
        localHubProviderMock
            .Setup(h => h.RegisterHandler<string, string, long>(
                InletHubConstants.ProjectionUpdatedMethod,
                It.IsAny<Func<string, string, long, Task>>()))
            .Callback<string, Func<string, string, long, Task>>((_, cb) => capturedCallback = cb)
            .Returns(Mock.Of<IDisposable>());

        InletSignalREffect localEffect = new(
            lazyStore,
            localHubProviderMock.Object,
            fetcherMock.Object,
            registry);

        // Act - invoke with registered path but not subscribed
        Assert.NotNull(capturedCallback);
        await capturedCallback(TestProjectionPath, TestEntityId, 1L);

        // Assert - store should not be called since not subscribed
        storeMock.Verify(s => s.Dispatch(It.IsAny<IAction>()), Times.Never);

        await localEffect.DisposeAsync();
    }

    /// <summary>
    ///     Verifies that OnReconnectedAsync handles empty subscriptions gracefully.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("OnReconnected")]
    public async Task OnReconnectedHandlesEmptySubscriptions()
    {
        // Arrange - capture the reconnection callback
        Func<string?, Task>? capturedCallback = null;
        Mock<IHubConnectionProvider> localHubProviderMock = new();
        localHubProviderMock
            .Setup(h => h.RegisterHandler<string, string, long>(
                It.IsAny<string>(),
                It.IsAny<Func<string, string, long, Task>>()))
            .Returns(Mock.Of<IDisposable>());
        localHubProviderMock
            .Setup(h => h.OnReconnected(It.IsAny<Func<string?, Task>>()))
            .Callback<Func<string?, Task>>(cb => capturedCallback = cb);

        InletSignalREffect localEffect = new(
            lazyStore,
            localHubProviderMock.Object,
            fetcherMock.Object,
            registry);

        // Act - invoke reconnection with no subscriptions
        Assert.NotNull(capturedCallback);
        await capturedCallback("new-connection-id");

        // Assert - should complete without errors, no store dispatch
        storeMock.Verify(s => s.Dispatch(It.IsAny<IAction>()), Times.Never);

        await localEffect.DisposeAsync();
    }

    /// <summary>
    ///     Verifies that HandleAsync yields loading action first for refresh.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("HandleAsync")]
    public async Task HandleRefreshYieldsLoadingActionFirst()
    {
        // Arrange
        hubProviderMock.Setup(h => h.EnsureConnectedAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        TestProjection projection = new("test", 42);
        fetcherMock
            .Setup(f => f.FetchAsync(typeof(TestProjection), TestEntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectionFetchResult(projection, 1L));

        RefreshProjectionAction<TestProjection> action = new(TestEntityId);

        // Act
        List<IAction> results = [];
        await foreach (IAction resultAction in effect!.HandleAsync(action, CancellationToken.None))
        {
            results.Add(resultAction);
        }

        // Assert - first action should be loading
        Assert.NotEmpty(results);
        Assert.IsType<ProjectionLoadingAction<TestProjection>>(results[0]);
    }

    /// <summary>
    ///     Verifies that HandleAsync yields updated action after successful refresh.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("HandleAsync")]
    public async Task HandleRefreshYieldsUpdatedActionOnSuccess()
    {
        // Arrange
        hubProviderMock.Setup(h => h.EnsureConnectedAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        TestProjection projection = new("test", 42);
        fetcherMock
            .Setup(f => f.FetchAsync(typeof(TestProjection), TestEntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectionFetchResult(projection, 1L));

        RefreshProjectionAction<TestProjection> action = new(TestEntityId);

        // Act
        List<IAction> results = [];
        await foreach (IAction resultAction in effect!.HandleAsync(action, CancellationToken.None))
        {
            results.Add(resultAction);
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.IsType<ProjectionLoadingAction<TestProjection>>(results[0]);
        ProjectionUpdatedAction<TestProjection> updatedAction = Assert.IsType<ProjectionUpdatedAction<TestProjection>>(results[1]);
        Assert.Equal(TestEntityId, updatedAction.EntityId);
        Assert.Equal(projection, updatedAction.Data);
        Assert.Equal(1L, updatedAction.Version);
    }

    /// <summary>
    ///     Verifies that HandleAsync yields error action when fetch throws.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("HandleAsync")]
    public async Task HandleRefreshYieldsErrorActionOnFetchException()
    {
        // Arrange
        hubProviderMock.Setup(h => h.EnsureConnectedAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        InvalidOperationException expectedException = new("Fetch failed");
        fetcherMock
            .Setup(f => f.FetchAsync(typeof(TestProjection), TestEntityId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        RefreshProjectionAction<TestProjection> action = new(TestEntityId);

        // Act
        List<IAction> results = [];
        await foreach (IAction resultAction in effect!.HandleAsync(action, CancellationToken.None))
        {
            results.Add(resultAction);
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.IsType<ProjectionLoadingAction<TestProjection>>(results[0]);
        ProjectionErrorAction<TestProjection> errorAction = Assert.IsType<ProjectionErrorAction<TestProjection>>(results[1]);
        Assert.Equal(TestEntityId, errorAction.EntityId);
        Assert.Same(expectedException, errorAction.Error);
    }

    /// <summary>
    ///     Verifies that HandleAsync yields error action when fetcher returns null.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("HandleAsync")]
    public async Task HandleRefreshYieldsErrorActionOnNullResult()
    {
        // Arrange
        hubProviderMock.Setup(h => h.EnsureConnectedAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        fetcherMock
            .Setup(f => f.FetchAsync(typeof(TestProjection), TestEntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectionFetchResult?)null);

        RefreshProjectionAction<TestProjection> action = new(TestEntityId);

        // Act
        List<IAction> results = [];
        await foreach (IAction resultAction in effect!.HandleAsync(action, CancellationToken.None))
        {
            results.Add(resultAction);
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.IsType<ProjectionLoadingAction<TestProjection>>(results[0]);
        ProjectionErrorAction<TestProjection> errorAction = Assert.IsType<ProjectionErrorAction<TestProjection>>(results[1]);
        Assert.Equal(TestEntityId, errorAction.EntityId);
        Assert.Contains("No fetcher registered", errorAction.Error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies that HandleAsync stops when cancellation is requested during fetch.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("HandleAsync")]
    public async Task HandleRefreshStopsOnCancellation()
    {
        // Arrange
        hubProviderMock.Setup(h => h.EnsureConnectedAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        fetcherMock
            .Setup(f => f.FetchAsync(typeof(TestProjection), TestEntityId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        RefreshProjectionAction<TestProjection> action = new(TestEntityId);

        // Act
        List<IAction> results = [];
        await foreach (IAction resultAction in effect!.HandleAsync(action, CancellationToken.None))
        {
            results.Add(resultAction);
        }

        // Assert - only loading action, no error because cancellation is expected
        Assert.Single(results);
        Assert.IsType<ProjectionLoadingAction<TestProjection>>(results[0]);
    }
}