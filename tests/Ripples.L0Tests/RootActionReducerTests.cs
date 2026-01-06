using System;

using Allure.Xunit.Attributes;

using Mississippi.Ripples.Abstractions;
using Mississippi.Ripples.Abstractions.Actions;
using Mississippi.Ripples.Abstractions.State;


namespace Mississippi.Ripples.L0Tests;

/// <summary>
///     Tests for <see cref="RootActionReducer{TState}" />.
/// </summary>
[AllureParentSuite("Mississippi.Ripples")]
[AllureSuite("Reducers")]
[AllureSubSuite("RootActionReducer")]
public sealed class RootActionReducerTests
{
    private sealed record SetPanelAction(string Panel) : IAction;

    private sealed class SetPanelReducer : ActionReducer<SetPanelAction, TestSidebarState>
    {
        protected override TestSidebarState ReduceCore(
            TestSidebarState state,
            SetPanelAction action
        ) =>
            state with
            {
                Panel = action.Panel,
            };
    }

    private sealed record TestSidebarState : IFeatureState
    {
        public static string FeatureKey => "sidebar";

        public bool IsOpen { get; init; }

        public string Panel { get; init; } = string.Empty;
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
    ///     Verifies that constructor throws when reducers is null.
    /// </summary>
    [Fact]
    [AllureFeature("Input Validation")]
    public void ConstructorThrowsWhenReducersIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RootActionReducer<TestSidebarState>(null!));
    }

    /// <summary>
    ///     Verifies that Reduce applies a matching action using the registered reducer.
    /// </summary>
    [Fact]
    [AllureFeature("Reduce")]
    public void ReduceAppliesMatchingAction()
    {
        // Arrange
        ToggleSidebarReducer toggleReducer = new();
        IRootActionReducer<TestSidebarState> rootReducer = new RootActionReducer<TestSidebarState>([toggleReducer]);
        TestSidebarState state = new()
        {
            IsOpen = false,
        };
        ToggleSidebarAction action = new();

        // Act
        TestSidebarState result = rootReducer.Reduce(state, action);

        // Assert
        Assert.NotSame(state, result);
        Assert.True(result.IsOpen);
    }

    /// <summary>
    ///     Verifies that Reduce applies multiple reducers for different action types.
    /// </summary>
    [Fact]
    [AllureFeature("Reduce")]
    public void ReduceAppliesMultipleReducersForDifferentActionTypes()
    {
        // Arrange
        ToggleSidebarReducer toggleReducer = new();
        SetPanelReducer setPanelReducer = new();
        IRootActionReducer<TestSidebarState> rootReducer =
            new RootActionReducer<TestSidebarState>([toggleReducer, setPanelReducer]);
        TestSidebarState state = new()
        {
            IsOpen = false,
            Panel = "default",
        };

        // Act
        TestSidebarState afterToggle = rootReducer.Reduce(state, new ToggleSidebarAction());
        TestSidebarState afterSetPanel = rootReducer.Reduce(afterToggle, new SetPanelAction("channels"));

        // Assert
        Assert.True(afterToggle.IsOpen);
        Assert.Equal("default", afterToggle.Panel);
        Assert.True(afterSetPanel.IsOpen);
        Assert.Equal("channels", afterSetPanel.Panel);
    }

    /// <summary>
    ///     Verifies that Reduce returns original state when no reducer matches the action.
    /// </summary>
    [Fact]
    [AllureFeature("Reduce")]
    public void ReduceReturnsOriginalStateWhenNoReducerMatches()
    {
        // Arrange
        ToggleSidebarReducer toggleReducer = new();
        IRootActionReducer<TestSidebarState> rootReducer = new RootActionReducer<TestSidebarState>([toggleReducer]);
        TestSidebarState state = new()
        {
            IsOpen = true,
        };
        UnrelatedAction action = new();

        // Act
        TestSidebarState result = rootReducer.Reduce(state, action);

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
        IRootActionReducer<TestSidebarState> rootReducer =
            new RootActionReducer<TestSidebarState>([new ToggleSidebarReducer()]);
        TestSidebarState state = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => rootReducer.Reduce(state, null!));
    }

    /// <summary>
    ///     Verifies that Reduce works with empty reducer collection.
    /// </summary>
    [Fact]
    [AllureFeature("Edge Cases")]
    public void ReduceWorksWithEmptyReducerCollection()
    {
        // Arrange
        IRootActionReducer<TestSidebarState> rootReducer = new RootActionReducer<TestSidebarState>([]);
        TestSidebarState state = new()
        {
            IsOpen = true,
        };
        ToggleSidebarAction action = new();

        // Act
        TestSidebarState result = rootReducer.Reduce(state, action);

        // Assert
        Assert.Same(state, result);
    }
}