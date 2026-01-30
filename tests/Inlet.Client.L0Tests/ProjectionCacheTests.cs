using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Inlet.Client.Reducers;
using Mississippi.Reservoir;
using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Inlet.Client.L0Tests;

/// <summary>
///     Tests for <see cref="ProjectionCache" />.
/// </summary>
public sealed class ProjectionCacheTests : IDisposable
{
    private readonly ServiceProvider serviceProvider;

    private readonly Store store;

    private readonly ProjectionCache sut;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionCacheTests" /> class.
    /// </summary>
    public ProjectionCacheTests()
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
        sut = new(store);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        store.Dispose();
        serviceProvider.Dispose();
    }

    /// <summary>
    ///     Test projection record for testing purposes.
    /// </summary>
    private sealed record TestProjection(string Name, int Value = 0);

    /// <summary>
    ///     Constructor should throw when store is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenStoreIsNull() =>
        Assert.Throws<ArgumentNullException>(() => new ProjectionCache(null!));

    /// <summary>
    ///     GetProjectionError returns error after dispatch.
    /// </summary>
    [Fact]
    public void GetProjectionErrorReturnsErrorAfterDispatch()
    {
        // Arrange
        InvalidOperationException error = new("Test error");
        store.Dispatch(new ProjectionErrorAction<TestProjection>("entity-1", error));

        // Act
        Exception? result = sut.GetProjectionError<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test error", result.Message);
    }

    /// <summary>
    ///     GetProjectionError returns null for non-existent entity.
    /// </summary>
    [Fact]
    public void GetProjectionErrorReturnsNullForNonExistentEntity()
    {
        // Act
        Exception? result = sut.GetProjectionError<TestProjection>("non-existent");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     GetProjectionError throws when entityId is null.
    /// </summary>
    [Fact]
    public void GetProjectionErrorThrowsArgumentNullExceptionWhenEntityIdIsNull() =>
        Assert.Throws<ArgumentNullException>(() => sut.GetProjectionError<TestProjection>(null!));

    /// <summary>
    ///     GetProjection returns data after dispatch.
    /// </summary>
    [Fact]
    public void GetProjectionReturnsDataAfterDispatch()
    {
        // Arrange
        TestProjection projection = new("Test", 42);
        store.Dispatch(new ProjectionLoadedAction<TestProjection>("entity-1", projection, 5L));

        // Act
        TestProjection? result = sut.GetProjection<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal(42, result.Value);
    }

    /// <summary>
    ///     GetProjection returns null for non-existent entity.
    /// </summary>
    [Fact]
    public void GetProjectionReturnsNullForNonExistentEntity()
    {
        // Act
        TestProjection? result = sut.GetProjection<TestProjection>("non-existent");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     GetProjectionState includes connected state.
    /// </summary>
    [Fact]
    public void GetProjectionStateIncludesConnectedState()
    {
        // Arrange
        store.Dispatch(new ProjectionConnectionChangedAction<TestProjection>("entity-1", true));

        // Act
        IProjectionState<TestProjection>? state = sut.GetProjectionState<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(state);
        Assert.True(state.IsConnected);
    }

    /// <summary>
    ///     GetProjectionState includes error information.
    /// </summary>
    [Fact]
    public void GetProjectionStateIncludesErrorInformation()
    {
        // Arrange
        InvalidOperationException error = new("Test error");
        store.Dispatch(new ProjectionErrorAction<TestProjection>("entity-1", error));

        // Act
        IProjectionState<TestProjection>? state = sut.GetProjectionState<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(state);
        Assert.NotNull(state.ErrorException);
        Assert.Equal("Test error", state.ErrorException.Message);
    }

    /// <summary>
    ///     GetProjectionState includes loading state.
    /// </summary>
    [Fact]
    public void GetProjectionStateIncludesLoadingState()
    {
        // Arrange
        store.Dispatch(new ProjectionLoadingAction<TestProjection>("entity-1"));

        // Act
        IProjectionState<TestProjection>? state = sut.GetProjectionState<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(state);
        Assert.True(state.IsLoading);
    }

    /// <summary>
    ///     GetProjectionState returns null for non-existent entity.
    /// </summary>
    [Fact]
    public void GetProjectionStateReturnsNullWhenNotExists()
    {
        // Act
        IProjectionState<TestProjection>? state = sut.GetProjectionState<TestProjection>("non-existent");

        // Assert
        Assert.Null(state);
    }

    /// <summary>
    ///     GetProjectionState returns state after dispatch.
    /// </summary>
    [Fact]
    public void GetProjectionStateReturnsStateWhenExists()
    {
        // Arrange
        TestProjection projection = new("Test", 42);
        store.Dispatch(new ProjectionLoadedAction<TestProjection>("entity-1", projection, 5L));

        // Act
        IProjectionState<TestProjection>? state = sut.GetProjectionState<TestProjection>("entity-1");

        // Assert
        Assert.NotNull(state);
        Assert.NotNull(state.Data);
        Assert.Equal("Test", state.Data.Name);
        Assert.Equal(5L, state.Version);
    }

    /// <summary>
    ///     GetProjectionState throws when entityId is null.
    /// </summary>
    [Fact]
    public void GetProjectionStateThrowsArgumentNullExceptionWhenEntityIdIsNull() =>
        Assert.Throws<ArgumentNullException>(() => sut.GetProjectionState<TestProjection>(null!));

    /// <summary>
    ///     GetProjection throws when entityId is null.
    /// </summary>
    [Fact]
    public void GetProjectionThrowsArgumentNullExceptionWhenEntityIdIsNull() =>
        Assert.Throws<ArgumentNullException>(() => sut.GetProjection<TestProjection>(null!));

    /// <summary>
    ///     GetProjectionVersion returns -1 for non-existent entity.
    /// </summary>
    [Fact]
    public void GetProjectionVersionReturnsNegativeOneForNonExistentEntity()
    {
        // Act
        long result = sut.GetProjectionVersion<TestProjection>("non-existent");

        // Assert
        Assert.Equal(-1, result);
    }

    /// <summary>
    ///     GetProjectionVersion returns version after dispatch.
    /// </summary>
    [Fact]
    public void GetProjectionVersionReturnsVersionAfterDispatch()
    {
        // Arrange
        TestProjection projection = new("Test", 42);
        store.Dispatch(new ProjectionLoadedAction<TestProjection>("entity-1", projection, 10L));

        // Act
        long result = sut.GetProjectionVersion<TestProjection>("entity-1");

        // Assert
        Assert.Equal(10L, result);
    }

    /// <summary>
    ///     GetProjectionVersion throws when entityId is null.
    /// </summary>
    [Fact]
    public void GetProjectionVersionThrowsArgumentNullExceptionWhenEntityIdIsNull() =>
        Assert.Throws<ArgumentNullException>(() => sut.GetProjectionVersion<TestProjection>(null!));

    /// <summary>
    ///     IsProjectionConnected returns false for non-existent entity.
    /// </summary>
    [Fact]
    public void IsProjectionConnectedReturnsFalseForNonExistentEntity()
    {
        // Act
        bool result = sut.IsProjectionConnected<TestProjection>("non-existent");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    ///     IsProjectionConnected returns true after dispatch.
    /// </summary>
    [Fact]
    public void IsProjectionConnectedReturnsTrueAfterDispatch()
    {
        // Arrange
        store.Dispatch(new ProjectionConnectionChangedAction<TestProjection>("entity-1", true));

        // Act
        bool result = sut.IsProjectionConnected<TestProjection>("entity-1");

        // Assert
        Assert.True(result);
    }

    /// <summary>
    ///     IsProjectionConnected throws when entityId is null.
    /// </summary>
    [Fact]
    public void IsProjectionConnectedThrowsArgumentNullExceptionWhenEntityIdIsNull() =>
        Assert.Throws<ArgumentNullException>(() => sut.IsProjectionConnected<TestProjection>(null!));

    /// <summary>
    ///     IsProjectionLoading returns false for non-existent entity.
    /// </summary>
    [Fact]
    public void IsProjectionLoadingReturnsFalseForNonExistentEntity()
    {
        // Act
        bool result = sut.IsProjectionLoading<TestProjection>("non-existent");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    ///     IsProjectionLoading returns true after dispatch.
    /// </summary>
    [Fact]
    public void IsProjectionLoadingReturnsTrueAfterDispatch()
    {
        // Arrange
        store.Dispatch(new ProjectionLoadingAction<TestProjection>("entity-1"));

        // Act
        bool result = sut.IsProjectionLoading<TestProjection>("entity-1");

        // Assert
        Assert.True(result);
    }

    /// <summary>
    ///     IsProjectionLoading throws when entityId is null.
    /// </summary>
    [Fact]
    public void IsProjectionLoadingThrowsArgumentNullExceptionWhenEntityIdIsNull() =>
        Assert.Throws<ArgumentNullException>(() => sut.IsProjectionLoading<TestProjection>(null!));

    /// <summary>
    ///     Projection updates should reflect latest state.
    /// </summary>
    [Fact]
    public void ProjectionUpdatesReflectLatestState()
    {
        // Arrange
        TestProjection initialProjection = new("Initial", 1);
        store.Dispatch(new ProjectionLoadedAction<TestProjection>("entity-1", initialProjection, 5L));

        // Act
        TestProjection updatedProjection = new("Updated", 99);
        store.Dispatch(new ProjectionUpdatedAction<TestProjection>("entity-1", updatedProjection, 10L));

        // Assert
        TestProjection? result = sut.GetProjection<TestProjection>("entity-1");
        Assert.NotNull(result);
        Assert.Equal("Updated", result.Name);
        Assert.Equal(99, result.Value);
        Assert.Equal(10L, sut.GetProjectionVersion<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     State changes should be visible immediately after dispatch.
    /// </summary>
    [Fact]
    public void StateChangesAreVisibleImmediatelyAfterDispatch()
    {
        // Act
        store.Dispatch(new ProjectionLoadingAction<TestProjection>("entity-1"));
        bool isLoading = sut.IsProjectionLoading<TestProjection>("entity-1");
        store.Dispatch(new ProjectionLoadedAction<TestProjection>("entity-1", new("Test"), 5L));
        bool isLoadingAfter = sut.IsProjectionLoading<TestProjection>("entity-1");

        // Assert
        Assert.True(isLoading);
        Assert.False(isLoadingAfter);
    }
}