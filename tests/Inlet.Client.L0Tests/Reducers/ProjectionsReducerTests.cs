using System;

using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Inlet.Client.Reducers;


namespace Mississippi.Inlet.Client.L0Tests.Reducers;

/// <summary>
///     Tests for <see cref="ProjectionsReducer" />.
/// </summary>
public sealed class ProjectionsReducerTests
{
    /// <summary>
    ///     Test projection record for unit tests.
    /// </summary>
    private sealed record TestProjection(string Name, int Value = 0);

    /// <summary>
    ///     Multiple entities should be tracked independently.
    /// </summary>
    [Fact]
    public void MultipleEntitiesAreTrackedIndependently()
    {
        // Arrange
        ProjectionsFeatureState state = new();

        // Act
        state = ProjectionsReducer.ReduceLoaded(
            state,
            new ProjectionLoadedAction<TestProjection>("entity-1", new("First", 1), 10L));
        state = ProjectionsReducer.ReduceLoaded(
            state,
            new ProjectionLoadedAction<TestProjection>("entity-2", new("Second", 2), 20L));
        state = ProjectionsReducer.ReduceLoading(state, new ProjectionLoadingAction<TestProjection>("entity-1"));

        // Assert
        Assert.True(state.IsProjectionLoading<TestProjection>("entity-1"));
        Assert.False(state.IsProjectionLoading<TestProjection>("entity-2"));
        Assert.Equal("Second", state.GetProjection<TestProjection>("entity-2")!.Name);
        Assert.Equal(20L, state.GetProjectionVersion<TestProjection>("entity-2"));
    }

    /// <summary>
    ///     ReduceConnectionChanged should set IsConnected to false when action.IsConnected is false.
    /// </summary>
    [Fact]
    public void ReduceConnectionChangedSetsConnectedToFalse()
    {
        // Arrange
        ProjectionsFeatureState state = new();

        // First set connected
        state = ProjectionsReducer.ReduceConnectionChanged(
            state,
            new ProjectionConnectionChangedAction<TestProjection>("entity-1", true));
        ProjectionConnectionChangedAction<TestProjection> action = new("entity-1", false);

        // Act
        ProjectionsFeatureState result = ProjectionsReducer.ReduceConnectionChanged(state, action);

        // Assert
        Assert.False(result.IsProjectionConnected<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     ReduceConnectionChanged should set IsConnected to true when action.IsConnected is true.
    /// </summary>
    [Fact]
    public void ReduceConnectionChangedSetsConnectedToTrue()
    {
        // Arrange
        ProjectionsFeatureState state = new();
        ProjectionConnectionChangedAction<TestProjection> action = new("entity-1", true);

        // Act
        ProjectionsFeatureState result = ProjectionsReducer.ReduceConnectionChanged(state, action);

        // Assert
        Assert.True(result.IsProjectionConnected<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     ReduceConnectionChanged should throw when action is null.
    /// </summary>
    [Fact]
    public void ReduceConnectionChangedThrowsWhenActionIsNull() =>
        Assert.Throws<ArgumentNullException>(() => ProjectionsReducer.ReduceConnectionChanged<TestProjection>(
            new(),
            null!));

    /// <summary>
    ///     ReduceConnectionChanged should throw when state is null.
    /// </summary>
    [Fact]
    public void ReduceConnectionChangedThrowsWhenStateIsNull() =>
        Assert.Throws<ArgumentNullException>(() => ProjectionsReducer.ReduceConnectionChanged<TestProjection>(
            null!,
            new("entity-1", true)));

    /// <summary>
    ///     ReduceError should set Error and clear IsLoading.
    /// </summary>
    [Fact]
    public void ReduceErrorSetsErrorAndClearsLoading()
    {
        // Arrange
        ProjectionsFeatureState state = new();

        // First set loading
        state = ProjectionsReducer.ReduceLoading(state, new ProjectionLoadingAction<TestProjection>("entity-1"));
        InvalidOperationException error = new("Test error");
        ProjectionErrorAction<TestProjection> action = new("entity-1", error);

        // Act
        ProjectionsFeatureState result = ProjectionsReducer.ReduceError(state, action);

        // Assert
        Assert.NotNull(result.GetProjectionError<TestProjection>("entity-1"));
        Assert.Equal("Test error", result.GetProjectionError<TestProjection>("entity-1")!.Message);
        Assert.False(result.IsProjectionLoading<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     ReduceError should throw when action is null.
    /// </summary>
    [Fact]
    public void ReduceErrorThrowsWhenActionIsNull() =>
        Assert.Throws<ArgumentNullException>(() => ProjectionsReducer.ReduceError<TestProjection>(new(), null!));

    /// <summary>
    ///     ReduceError should throw when state is null.
    /// </summary>
    [Fact]
    public void ReduceErrorThrowsWhenStateIsNull() =>
        Assert.Throws<ArgumentNullException>(() => ProjectionsReducer.ReduceError<TestProjection>(
            null!,
            new("entity-1", new InvalidOperationException())));

    /// <summary>
    ///     ReduceLoaded should handle null data.
    /// </summary>
    [Fact]
    public void ReduceLoadedHandlesNullData()
    {
        // Arrange
        ProjectionsFeatureState state = new();
        ProjectionLoadedAction<TestProjection> action = new("entity-1", null, 5L);

        // Act
        ProjectionsFeatureState result = ProjectionsReducer.ReduceLoaded(state, action);

        // Assert
        Assert.Null(result.GetProjection<TestProjection>("entity-1"));
        Assert.Equal(5L, result.GetProjectionVersion<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     ReduceLoaded should set Data, Version, clear IsLoading and Error.
    /// </summary>
    [Fact]
    public void ReduceLoadedSetsDataAndVersionClearsLoadingAndError()
    {
        // Arrange
        ProjectionsFeatureState state = new();

        // First set loading and error
        state = ProjectionsReducer.ReduceLoading(state, new ProjectionLoadingAction<TestProjection>("entity-1"));
        state = ProjectionsReducer.ReduceError(
            state,
            new ProjectionErrorAction<TestProjection>("entity-1", new InvalidOperationException("old error")));
        TestProjection projection = new("Test", 42);
        ProjectionLoadedAction<TestProjection> action = new("entity-1", projection, 10L);

        // Act
        ProjectionsFeatureState result = ProjectionsReducer.ReduceLoaded(state, action);

        // Assert
        Assert.NotNull(result.GetProjection<TestProjection>("entity-1"));
        Assert.Equal("Test", result.GetProjection<TestProjection>("entity-1")!.Name);
        Assert.Equal(42, result.GetProjection<TestProjection>("entity-1")!.Value);
        Assert.Equal(10L, result.GetProjectionVersion<TestProjection>("entity-1"));
        Assert.False(result.IsProjectionLoading<TestProjection>("entity-1"));
        Assert.Null(result.GetProjectionError<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     ReduceLoaded should throw when action is null.
    /// </summary>
    [Fact]
    public void ReduceLoadedThrowsWhenActionIsNull() =>
        Assert.Throws<ArgumentNullException>(() => ProjectionsReducer.ReduceLoaded<TestProjection>(new(), null!));

    /// <summary>
    ///     ReduceLoaded should throw when state is null.
    /// </summary>
    [Fact]
    public void ReduceLoadedThrowsWhenStateIsNull() =>
        Assert.Throws<ArgumentNullException>(() => ProjectionsReducer.ReduceLoaded<TestProjection>(
            null!,
            new("entity-1", null, 5L)));

    /// <summary>
    ///     ReduceLoading should set IsLoading and clear Error.
    /// </summary>
    [Fact]
    public void ReduceLoadingSetsLoadingAndClearsError()
    {
        // Arrange
        ProjectionsFeatureState state = new();

        // First set error
        state = ProjectionsReducer.ReduceError(
            state,
            new ProjectionErrorAction<TestProjection>("entity-1", new InvalidOperationException("old error")));
        ProjectionLoadingAction<TestProjection> action = new("entity-1");

        // Act
        ProjectionsFeatureState result = ProjectionsReducer.ReduceLoading(state, action);

        // Assert
        Assert.True(result.IsProjectionLoading<TestProjection>("entity-1"));
        Assert.Null(result.GetProjectionError<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     ReduceLoading should throw when action is null.
    /// </summary>
    [Fact]
    public void ReduceLoadingThrowsWhenActionIsNull() =>
        Assert.Throws<ArgumentNullException>(() => ProjectionsReducer.ReduceLoading<TestProjection>(new(), null!));

    /// <summary>
    ///     ReduceLoading should throw when state is null.
    /// </summary>
    [Fact]
    public void ReduceLoadingThrowsWhenStateIsNull() =>
        Assert.Throws<ArgumentNullException>(() => ProjectionsReducer.ReduceLoading<TestProjection>(
            null!,
            new("entity-1")));

    /// <summary>
    ///     ReduceUpdated should handle null data.
    /// </summary>
    [Fact]
    public void ReduceUpdatedHandlesNullData()
    {
        // Arrange
        ProjectionsFeatureState state = new();

        // First set existing data
        state = ProjectionsReducer.ReduceLoaded(
            state,
            new ProjectionLoadedAction<TestProjection>("entity-1", new("Original", 1), 5L));
        ProjectionUpdatedAction<TestProjection> action = new("entity-1", null, 20L);

        // Act
        ProjectionsFeatureState result = ProjectionsReducer.ReduceUpdated(state, action);

        // Assert
        Assert.Null(result.GetProjection<TestProjection>("entity-1"));
        Assert.Equal(20L, result.GetProjectionVersion<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     ReduceUpdated should set Data, Version, clear IsLoading and Error.
    /// </summary>
    [Fact]
    public void ReduceUpdatedSetsDataAndVersionClearsLoadingAndError()
    {
        // Arrange
        ProjectionsFeatureState state = new();

        // First set some existing state
        state = ProjectionsReducer.ReduceLoaded(
            state,
            new ProjectionLoadedAction<TestProjection>("entity-1", new("Original", 1), 5L));
        TestProjection updatedProjection = new("Updated", 99);
        ProjectionUpdatedAction<TestProjection> action = new("entity-1", updatedProjection, 15L);

        // Act
        ProjectionsFeatureState result = ProjectionsReducer.ReduceUpdated(state, action);

        // Assert
        Assert.NotNull(result.GetProjection<TestProjection>("entity-1"));
        Assert.Equal("Updated", result.GetProjection<TestProjection>("entity-1")!.Name);
        Assert.Equal(99, result.GetProjection<TestProjection>("entity-1")!.Value);
        Assert.Equal(15L, result.GetProjectionVersion<TestProjection>("entity-1"));
        Assert.False(result.IsProjectionLoading<TestProjection>("entity-1"));
        Assert.Null(result.GetProjectionError<TestProjection>("entity-1"));
    }

    /// <summary>
    ///     ReduceUpdated should throw when action is null.
    /// </summary>
    [Fact]
    public void ReduceUpdatedThrowsWhenActionIsNull() =>
        Assert.Throws<ArgumentNullException>(() => ProjectionsReducer.ReduceUpdated<TestProjection>(new(), null!));

    /// <summary>
    ///     ReduceUpdated should throw when state is null.
    /// </summary>
    [Fact]
    public void ReduceUpdatedThrowsWhenStateIsNull() =>
        Assert.Throws<ArgumentNullException>(() => ProjectionsReducer.ReduceUpdated<TestProjection>(
            null!,
            new("entity-1", null, 5L)));

    /// <summary>
    ///     Reducers should not mutate the original state (immutability check).
    /// </summary>
    [Fact]
    public void ReducersDoNotMutateOriginalState()
    {
        // Arrange
        ProjectionsFeatureState original = new();

        // Act
        ProjectionsFeatureState afterLoading = ProjectionsReducer.ReduceLoading(
            original,
            new ProjectionLoadingAction<TestProjection>("entity-1"));

        // Assert - original should remain unchanged
        Assert.False(original.IsProjectionLoading<TestProjection>("entity-1"));
        Assert.True(afterLoading.IsProjectionLoading<TestProjection>("entity-1"));
        Assert.NotSame(original, afterLoading);
    }
}