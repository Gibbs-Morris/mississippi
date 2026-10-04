using System;

using Mississippi.Reservoir.Client.BuiltIn.Lifecycle.Actions;
using Mississippi.Reservoir.Client.BuiltIn.Lifecycle.Reducers;
using Mississippi.Reservoir.Client.BuiltIn.Lifecycle.State;


namespace Mississippi.Reservoir.Client.L0Tests.BuiltIn.Lifecycle.Reducers;

/// <summary>
///     Unit tests for <see cref="LifecycleReducers" />.
/// </summary>
public sealed class LifecycleReducersTests
{
    /// <summary>
    ///     Verifies full lifecycle transition from NotStarted to Ready.
    /// </summary>
    [Fact]
    public void FullLifecycleFromNotStartedToReadyShouldTrackTimestamps()
    {
        // Arrange
        DateTimeOffset initTime = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        DateTimeOffset readyTime = new(2024, 1, 15, 10, 30, 5, TimeSpan.Zero);
        LifecycleState state = new()
        {
            Phase = LifecyclePhase.NotStarted,
            InitializedAt = null,
            ReadyAt = null,
        };

        // Act
        LifecycleState afterInit = LifecycleReducers.OnAppInit(state, new(initTime));
        LifecycleState afterReady = LifecycleReducers.OnAppReady(afterInit, new(readyTime));

        // Assert
        Assert.Equal(LifecyclePhase.Ready, afterReady.Phase);
        Assert.Equal(initTime, afterReady.InitializedAt);
        Assert.Equal(readyTime, afterReady.ReadyAt);
    }

    /// <summary>
    ///     Verifies that the initial state has the correct feature key.
    /// </summary>
    [Fact]
    public void InitialStateShouldHaveCorrectFeatureKey()
    {
        // Assert
        Assert.Equal("reservoir:lifecycle", LifecycleState.FeatureKey);
    }

    /// <summary>
    ///     Verifies LifecyclePhase enum has expected values.
    /// </summary>
    [Fact]
    public void LifecyclePhaseShouldHaveExpectedValues()
    {
        // Assert
        Assert.Equivalent(
            new[] { LifecyclePhase.NotStarted, LifecyclePhase.Initializing, LifecyclePhase.Ready },
            Enum.GetValues<LifecyclePhase>(),
            true);
    }

    /// <summary>
    ///     Verifies that OnAppInit does not change ReadyAt.
    /// </summary>
    [Fact]
    public void OnAppInitShouldNotSetReadyAt()
    {
        // Arrange
        DateTimeOffset initializedAt = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        LifecycleState initialState = new()
        {
            Phase = LifecyclePhase.NotStarted,
            InitializedAt = null,
            ReadyAt = null,
        };
        AppInitAction action = new(initializedAt);

        // Act
        LifecycleState result = LifecycleReducers.OnAppInit(initialState, action);

        // Assert
        Assert.Null(result.ReadyAt);
    }

    /// <summary>
    ///     Verifies that OnAppInit sets the InitializedAt timestamp.
    /// </summary>
    [Fact]
    public void OnAppInitShouldSetInitializedAtTimestamp()
    {
        // Arrange
        DateTimeOffset expectedTime = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        LifecycleState initialState = new()
        {
            Phase = LifecyclePhase.NotStarted,
            InitializedAt = null,
            ReadyAt = null,
        };
        AppInitAction action = new(expectedTime);

        // Act
        LifecycleState result = LifecycleReducers.OnAppInit(initialState, action);

        // Assert
        Assert.Equal(expectedTime, result.InitializedAt);
    }

    /// <summary>
    ///     Verifies that OnAppInit transitions phase to Initializing.
    /// </summary>
    [Fact]
    public void OnAppInitShouldSetPhaseToInitializing()
    {
        // Arrange
        LifecycleState initialState = new()
        {
            Phase = LifecyclePhase.NotStarted,
            InitializedAt = null,
            ReadyAt = null,
        };
        AppInitAction action = new(new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));

        // Act
        LifecycleState result = LifecycleReducers.OnAppInit(initialState, action);

        // Assert
        Assert.Equal(LifecyclePhase.Initializing, result.Phase);
    }

    /// <summary>
    ///     Verifies that OnAppReady preserves InitializedAt.
    /// </summary>
    [Fact]
    public void OnAppReadyShouldPreserveInitializedAt()
    {
        // Arrange
        DateTimeOffset initTime = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        LifecycleState initialState = new()
        {
            Phase = LifecyclePhase.Initializing,
            InitializedAt = initTime,
            ReadyAt = null,
        };
        AppReadyAction action = new(new(2024, 1, 15, 10, 31, 0, TimeSpan.Zero));

        // Act
        LifecycleState result = LifecycleReducers.OnAppReady(initialState, action);

        // Assert
        Assert.Equal(initTime, result.InitializedAt);
    }

    /// <summary>
    ///     Verifies that OnAppReady transitions phase to Ready.
    /// </summary>
    [Fact]
    public void OnAppReadyShouldSetPhaseToReady()
    {
        // Arrange
        DateTimeOffset initTime = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        LifecycleState initialState = new()
        {
            Phase = LifecyclePhase.Initializing,
            InitializedAt = initTime,
            ReadyAt = null,
        };
        AppReadyAction action = new(new(2024, 1, 15, 10, 31, 0, TimeSpan.Zero));

        // Act
        LifecycleState result = LifecycleReducers.OnAppReady(initialState, action);

        // Assert
        Assert.Equal(LifecyclePhase.Ready, result.Phase);
    }

    /// <summary>
    ///     Verifies that OnAppReady sets the ReadyAt timestamp.
    /// </summary>
    [Fact]
    public void OnAppReadyShouldSetReadyAtTimestamp()
    {
        // Arrange
        DateTimeOffset expectedTime = new(2024, 1, 15, 10, 31, 0, TimeSpan.Zero);
        DateTimeOffset initTime = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        LifecycleState initialState = new()
        {
            Phase = LifecyclePhase.Initializing,
            InitializedAt = initTime,
            ReadyAt = null,
        };
        AppReadyAction action = new(expectedTime);

        // Act
        LifecycleState result = LifecycleReducers.OnAppReady(initialState, action);

        // Assert
        Assert.Equal(expectedTime, result.ReadyAt);
    }
}