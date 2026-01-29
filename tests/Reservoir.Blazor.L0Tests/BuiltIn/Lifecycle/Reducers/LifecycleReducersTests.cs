using System;

using FluentAssertions;

using Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.Actions;
using Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.Reducers;
using Mississippi.Reservoir.Blazor.BuiltIn.Lifecycle.State;


namespace Mississippi.Reservoir.Blazor.L0Tests.BuiltIn.Lifecycle.Reducers;

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
        afterReady.Phase.Should().Be(LifecyclePhase.Ready);
        afterReady.InitializedAt.Should().Be(initTime);
        afterReady.ReadyAt.Should().Be(readyTime);
    }

    /// <summary>
    ///     Verifies that the initial state has the correct feature key.
    /// </summary>
    [Fact]
    public void InitialStateShouldHaveCorrectFeatureKey()
    {
        // Assert
        LifecycleState.FeatureKey.Should().Be("reservoir:lifecycle");
    }

    /// <summary>
    ///     Verifies LifecyclePhase enum has expected values.
    /// </summary>
    [Fact]
    public void LifecyclePhaseShouldHaveExpectedValues()
    {
        // Assert
        Enum.GetValues<LifecyclePhase>()
            .Should()
            .BeEquivalentTo([LifecyclePhase.NotStarted, LifecyclePhase.Initializing, LifecyclePhase.Ready]);
    }

    /// <summary>
    ///     Verifies that OnAppInit does not change ReadyAt.
    /// </summary>
    [Fact]
    public void OnAppInitShouldNotSetReadyAt()
    {
        // Arrange
        LifecycleState initialState = new()
        {
            Phase = LifecyclePhase.NotStarted,
            InitializedAt = null,
            ReadyAt = null,
        };
        AppInitAction action = new(DateTimeOffset.UtcNow);

        // Act
        LifecycleState result = LifecycleReducers.OnAppInit(initialState, action);

        // Assert
        result.ReadyAt.Should().BeNull();
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
        result.InitializedAt.Should().Be(expectedTime);
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
        result.Phase.Should().Be(LifecyclePhase.Initializing);
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
        result.InitializedAt.Should().Be(initTime);
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
        result.Phase.Should().Be(LifecyclePhase.Ready);
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
        result.ReadyAt.Should().Be(expectedTime);
    }
}