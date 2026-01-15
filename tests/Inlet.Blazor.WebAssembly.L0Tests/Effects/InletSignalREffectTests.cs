using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.AspNetCore.Components;
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
    private readonly InletSignalREffect? effect;

    private readonly Mock<IProjectionFetcher> fetcherMock = new();

    private readonly Mock<NavigationManager> navManagerMock = new();

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

        // Set up NavigationManager mock
        navManagerMock.Setup(n => n.ToAbsoluteUri(It.IsAny<string>()))
            .Returns<string>(path => new($"http://localhost{path}"));

        // Set up service provider with store
        services.AddSingleton(storeMock.Object);
        serviceProvider = services.BuildServiceProvider();

        // Create effect
        InletSignalREffectOptions options = new()
        {
            HubPath = TestHubPath,
        };
        effect = new(serviceProvider, navManagerMock.Object, fetcherMock.Object, registry, options);
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

    private const string TestEntityId = "test-entity-123";

    private const string TestHubPath = "/hubs/test-inlet";

    private const string TestProjectionPath = "test-projections";

    private const long TestVersion = 42L;

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
    ///     Verifies that constructor throws when navigation manager is null.
    /// </summary>
    [Fact]
    [AllureFeature("Constructor")]
    public void ConstructorThrowsWhenNavigationManagerIsNull()
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
            navManagerMock.Object,
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
            navManagerMock.Object,
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
        using ServiceProvider provider = services.BuildServiceProvider();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new InletSignalREffect(
            null!,
            navManagerMock.Object,
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
    ///     Verifies that RefreshProjectionAction yields error action when fetch fails.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("HandleAsync")]
    public async Task RefreshActionYieldsErrorActionWhenFetchFails()
    {
        // Arrange
        RefreshProjectionAction<TestProjection> action = new(TestEntityId);
        InvalidOperationException expectedError = new("Fetch failed");
        fetcherMock.Setup(f => f.FetchAsync(typeof(TestProjection), TestEntityId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedError);

        // Act
        List<IAction> results = [];
        await foreach (IAction resultAction in effect!.HandleAsync(action, CancellationToken.None))
        {
            results.Add(resultAction);
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.IsType<ProjectionErrorAction<TestProjection>>(results[1]);
        ProjectionErrorAction<TestProjection> errorAction = (ProjectionErrorAction<TestProjection>)results[1];
        Assert.Equal(TestEntityId, errorAction.EntityId);
        Assert.Equal(expectedError, errorAction.Error);
    }

    /// <summary>
    ///     Verifies that RefreshProjectionAction yields error action when fetch returns null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("HandleAsync")]
    public async Task RefreshActionYieldsErrorActionWhenFetchReturnsNull()
    {
        // Arrange
        RefreshProjectionAction<TestProjection> action = new(TestEntityId);
        fetcherMock.Setup(f => f.FetchAsync(typeof(TestProjection), TestEntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectionFetchResult?)null);

        // Act
        List<IAction> results = [];
        await foreach (IAction resultAction in effect!.HandleAsync(action, CancellationToken.None))
        {
            results.Add(resultAction);
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.IsType<ProjectionErrorAction<TestProjection>>(results[1]);
        ProjectionErrorAction<TestProjection> errorAction = (ProjectionErrorAction<TestProjection>)results[1];
        Assert.Equal(TestEntityId, errorAction.EntityId);
        Assert.IsType<InvalidOperationException>(errorAction.Error);
    }

    /// <summary>
    ///     Verifies that RefreshProjectionAction yields loading action first.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("HandleAsync")]
    public async Task RefreshActionYieldsLoadingActionFirst()
    {
        // Arrange
        RefreshProjectionAction<TestProjection> action = new(TestEntityId);
        TestProjection projection = new("Test", 123);
        ProjectionFetchResult fetchResult = new(projection, TestVersion);
        fetcherMock.Setup(f => f.FetchAsync(typeof(TestProjection), TestEntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fetchResult);

        // Act
        List<IAction> results = [];
        await foreach (IAction resultAction in effect!.HandleAsync(action, CancellationToken.None))
        {
            results.Add(resultAction);
        }

        // Assert
        Assert.NotEmpty(results);
        Assert.IsType<ProjectionLoadingAction<TestProjection>>(results[0]);
        ProjectionLoadingAction<TestProjection> loadingAction = (ProjectionLoadingAction<TestProjection>)results[0];
        Assert.Equal(TestEntityId, loadingAction.EntityId);
    }

    /// <summary>
    ///     Verifies that RefreshProjectionAction yields nothing when cancelled.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("HandleAsync")]
    public async Task RefreshActionYieldsNothingWhenCancelled()
    {
        // Arrange
        RefreshProjectionAction<TestProjection> action = new(TestEntityId);
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();
        fetcherMock.Setup(f => f.FetchAsync(typeof(TestProjection), TestEntityId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        List<IAction> results = [];
        await foreach (IAction resultAction in effect!.HandleAsync(action, cts.Token))
        {
            results.Add(resultAction);
        }

        // Assert
        Assert.Single(results); // Only loading action before cancellation
        Assert.IsType<ProjectionLoadingAction<TestProjection>>(results[0]);
    }

    /// <summary>
    ///     Verifies that RefreshProjectionAction yields updated action after successful fetch.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("HandleAsync")]
    public async Task RefreshActionYieldsUpdatedActionAfterSuccessfulFetch()
    {
        // Arrange
        RefreshProjectionAction<TestProjection> action = new(TestEntityId);
        TestProjection projection = new("Test", 123);
        ProjectionFetchResult fetchResult = new(projection, TestVersion);
        fetcherMock.Setup(f => f.FetchAsync(typeof(TestProjection), TestEntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fetchResult);

        // Act
        List<IAction> results = [];
        await foreach (IAction resultAction in effect!.HandleAsync(action, CancellationToken.None))
        {
            results.Add(resultAction);
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.IsType<ProjectionUpdatedAction<TestProjection>>(results[1]);
        ProjectionUpdatedAction<TestProjection> updatedAction = (ProjectionUpdatedAction<TestProjection>)results[1];
        Assert.Equal(TestEntityId, updatedAction.EntityId);
        Assert.Equal(projection, updatedAction.Data);
        Assert.Equal(TestVersion, updatedAction.Version);
    }
}