using System;

using Allure.Xunit.Attributes;

using Mississippi.Ripples.Abstractions.Actions;
using Mississippi.Ripples.Abstractions.State;


namespace Mississippi.Ripples.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="ActionReducer{TAction, TState}" />.
/// </summary>
[AllureParentSuite("Mississippi.Ripples.Abstractions")]
[AllureSuite("Reducers")]
[AllureSubSuite("ActionReducer")]
public sealed class ActionReducerTests
{
    private sealed record NoOpAction : IAction;

    private sealed class NoOpReducer : ActionReducer<NoOpAction, TestSidebarState>
    {
        protected override TestSidebarState ReduceCore(
            TestSidebarState state,
            NoOpAction action
        ) =>
            state;
    }

    private sealed record TestSidebarState : IFeatureState
    {
        public static string FeatureKey => "sidebar";

        public bool IsOpen { get; init; }
    }

    private sealed record ToggleSidebarAction : IAction;

    private sealed class ToggleSidebarReducer : ActionReducer<ToggleSidebarAction, TestSidebarState>
    {
        protected override TestSidebarState ReduceCore(
            TestSidebarState state,
            ToggleSidebarAction action
        ) =>
            state with
            {
                IsOpen = !state.IsOpen,
            };
    }

    private sealed record UnrelatedAction : IAction;

    /// <summary>
    ///     Verifies that Reduce applies the action and returns the new state.
    /// </summary>
    [Fact]
    [AllureFeature("Reduce")]
    public void ReduceAppliesActionAndReturnsNewState()
    {
        // Arrange
        ToggleSidebarReducer reducer = new();
        TestSidebarState state = new()
        {
            IsOpen = false,
        };
        ToggleSidebarAction action = new();

        // Act
        TestSidebarState result = reducer.Reduce(state, action);

        // Assert
        Assert.NotSame(state, result);
        Assert.True(result.IsOpen);
    }

    /// <summary>
    ///     Verifies that reducer can return the same state when no change is needed.
    /// </summary>
    [Fact]
    [AllureFeature("Reduce")]
    public void ReduceCanReturnSameStateWhenNoChangeNeeded()
    {
        // Arrange
        NoOpReducer reducer = new();
        TestSidebarState state = new()
        {
            IsOpen = true,
        };
        NoOpAction action = new();

        // Act
        TestSidebarState result = reducer.Reduce(state, action);

        // Assert
        Assert.Same(state, result);
    }

    /// <summary>
    ///     Verifies that Reduce throws when action is null.
    /// </summary>
    [Fact]
    [AllureFeature("Input Validation")]
    public void ReduceThrowsWhenActionIsNull()
    {
        // Arrange
        ToggleSidebarReducer reducer = new();
        TestSidebarState state = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => reducer.Reduce(state, null!));
    }

    /// <summary>
    ///     Verifies that TryReduce returns false when action type does not match.
    /// </summary>
    [Fact]
    [AllureFeature("TryReduce")]
    public void TryReduceReturnsFalseWhenActionTypeDoesNotMatch()
    {
        // Arrange
        ToggleSidebarReducer reducer = new();
        TestSidebarState state = new()
        {
            IsOpen = false,
        };
        IAction action = new UnrelatedAction();

        // Act
        bool result = reducer.TryReduce(state, action, out TestSidebarState newState);

        // Assert
        Assert.False(result);
        Assert.Equal(default, newState);
    }

    /// <summary>
    ///     Verifies that TryReduce returns true and outputs new state when action type matches.
    /// </summary>
    [Fact]
    [AllureFeature("TryReduce")]
    public void TryReduceReturnsTrueWhenActionTypeMatches()
    {
        // Arrange
        ToggleSidebarReducer reducer = new();
        TestSidebarState state = new()
        {
            IsOpen = false,
        };
        IAction action = new ToggleSidebarAction();

        // Act
        bool result = reducer.TryReduce(state, action, out TestSidebarState newState);

        // Assert
        Assert.True(result);
        Assert.True(newState.IsOpen);
    }

    /// <summary>
    ///     Verifies that TryReduce throws when action is null.
    /// </summary>
    [Fact]
    [AllureFeature("Input Validation")]
    public void TryReduceThrowsWhenActionIsNull()
    {
        // Arrange
        ToggleSidebarReducer reducer = new();
        TestSidebarState state = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => reducer.TryReduce(state, null!, out TestSidebarState _));
    }
}