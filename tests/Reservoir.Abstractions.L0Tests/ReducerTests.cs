using Allure.Xunit.Attributes;

using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="ActionReducerBase{TAction,TState}" />.
/// </summary>
[AllureParentSuite("Mississippi.Reservoir.Abstractions")]
[AllureSuite("Core")]
[AllureSubSuite("Reducer")]
public sealed class ReducerTests
{
    /// <summary>
    ///     Test action for unit tests.
    /// </summary>
    private sealed record IncrementAction : IAction;

    /// <summary>
    ///     Test reducer implementation.
    /// </summary>
    private sealed class IncrementActionReducer : ActionReducerBase<IncrementAction, TestState>
    {
        /// <inheritdoc />
        public override TestState Reduce(
            TestState state,
            IncrementAction action
        ) =>
            state with
            {
                Counter = state.Counter + 1,
            };
    }

    /// <summary>
    ///     Other action for testing non-matching scenarios.
    /// </summary>
    private sealed record OtherAction : IAction;

    /// <summary>
    ///     Test feature state for unit tests.
    /// </summary>
    private sealed record TestState : IFeatureState
    {
        /// <inheritdoc />
        public static string FeatureKey => "test-state";

        /// <summary>
        ///     Gets the counter value.
        /// </summary>
        public int Counter { get; init; }
    }

    /// <summary>
    ///     Reduce should apply transformation for matching action.
    /// </summary>
    [Fact]
    [AllureFeature("Reduction")]
    public void ReduceAppliesTransformation()
    {
        // Arrange
        IncrementActionReducer sut = new();
        TestState initialState = new()
        {
            Counter = 5,
        };

        // Act
        TestState result = sut.Reduce(initialState, new());

        // Assert
        Assert.Equal(6, result.Counter);
    }

    /// <summary>
    ///     TryReduce should return false and return original state for non-matching action type.
    /// </summary>
    [Fact]
    [AllureFeature("Type Matching")]
    public void TryReduceReturnsFalseForNonMatchingActionType()
    {
        // Arrange
        IncrementActionReducer sut = new();
        TestState initialState = new()
        {
            Counter = 10,
        };

        // Act
        bool result = sut.TryReduce(initialState, new OtherAction(), out TestState newState);

        // Assert
        Assert.False(result);
        Assert.Same(initialState, newState);
    }

    /// <summary>
    ///     TryReduce should return true and produce new state for matching action type.
    /// </summary>
    [Fact]
    [AllureFeature("Type Matching")]
    public void TryReduceReturnsTrueForMatchingActionType()
    {
        // Arrange
        IncrementActionReducer sut = new();
        TestState initialState = new()
        {
            Counter = 10,
        };

        // Act
        bool result = sut.TryReduce(initialState, new IncrementAction(), out TestState newState);

        // Assert
        Assert.True(result);
        Assert.Equal(11, newState.Counter);
    }
}