using System;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;

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

    private readonly ProjectionDtoRegistry registry = new();

    private readonly ServiceProvider serviceProvider;

    private readonly ServiceCollection services = new();

    private readonly Mock<IInletStore> storeMock = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="InletSignalREffectTests" /> class.
    /// </summary>
    public InletSignalREffectTests()
    {
        // Register test projection in registry
        registry.Register(TestProjectionPath, typeof(TestProjection));

        // Set up service provider with store
        services.AddSingleton(storeMock.Object);
        serviceProvider = services.BuildServiceProvider();

        // Set up hub provider mock
        hubProviderMock
            .Setup(h => h.RegisterHandler<string, string, long>(
                It.IsAny<string>(),
                It.IsAny<Func<string, string, long, Task>>()))
            .Returns(Mock.Of<IDisposable>());

        // Create effect
        effect = new(serviceProvider, hubProviderMock.Object, fetcherMock.Object, registry);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (effect is not null)
        {
            await effect.DisposeAsync();
        }

        await serviceProvider.DisposeAsync();
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
    ///     Verifies that constructor throws when hub connection provider is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenHubConnectionProviderIsNull()
    {
        // Arrange
        using ServiceProvider provider = services.BuildServiceProvider();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new InletSignalREffect(
            provider,
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
        // Arrange
        using ServiceProvider provider = services.BuildServiceProvider();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new InletSignalREffect(
            provider,
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
        // Arrange
        using ServiceProvider provider = services.BuildServiceProvider();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new InletSignalREffect(
            provider,
            hubProviderMock.Object,
            null!,
            registry));
    }

    /// <summary>
    ///     Verifies that constructor throws when service provider is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenServiceProviderIsNull()
    {
        // Arrange

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new InletSignalREffect(
            null!,
            hubProviderMock.Object,
            fetcherMock.Object,
            registry));
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
            serviceProvider,
            localHubProviderMock.Object,
            fetcherMock.Object,
            registry);

        // Act
        await localEffect.DisposeAsync();

        // Assert - callback registration was disposed
        callbackDisposableMock.Verify(d => d.Dispose(), Times.Once);
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
        localHubProviderMock
            .Setup(h => h.RegisterHandler<string, string, long>(
                InletHubConstants.ProjectionUpdatedMethod,
                It.IsAny<Func<string, string, long, Task>>()))
            .Returns(Mock.Of<IDisposable>());

        InletSignalREffect localEffect = new(
            serviceProvider,
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
            serviceProvider,
            localHubProviderMock.Object,
            fetcherMock.Object,
            registry);

        // Act & Assert
        localHubProviderMock.Verify(h => h.OnReconnected(It.IsAny<Func<string?, Task>>()), Times.Once);

        await localEffect.DisposeAsync();
    }
}
