using System;

using Allure.Xunit.Attributes;

using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.L0Tests;

/// <summary>
///     Tests for <see cref="DelegateActionReducer{TAction,TState}" />.
/// </summary>
[AllureParentSuite("Mississippi.Reservoir")]
[AllureSuite("Core")]
[AllureSubSuite("Delegate Action Reducer")]
public sealed class DelegateActionReducerTests
{
    /// <summary>
    ///     Test action for unit tests.
    /// </summary>
    private sealed record IncrementAction : IAction;

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
    ///     Constructor should throw ArgumentNullException when reduce delegate is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void ConstructorWithNullDelegateThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DelegateActionReducer<IncrementAction, TestState>(null!));
    }

    /// <summary>
    ///     DelegateActionReducer should invoke delegate when action matches.
    /// </summary>
    [Fact]
    [AllureFeature("Reduction")]
    public void ReduceInvokesDelegateWhenActionMatches()
    {
        // Arrange
        DelegateActionReducer<IncrementAction, TestState> sut = new((
            state,
            _
        ) => state with
        {
            Counter = state.Counter + 1,
        });
        TestState initialState = new()
        {
            Counter = 10,
        };

        // Act
        TestState result = sut.Reduce(initialState, new());

        // Assert
        Assert.Equal(11, result.Counter);
    }

    /// <summary>
    ///     TryReduce should return false and return original state for non-matching action type.
    /// </summary>
    [Fact]
    [AllureFeature("Type Matching")]
    public void TryReduceReturnsFalseAndReturnsOriginalStateForNonMatchingActionType()
    {
        // Arrange
        DelegateActionReducer<IncrementAction, TestState> sut = new((
            state,
            _
        ) => state with
        {
            Counter = state.Counter + 1,
        });
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
    public void TryReduceReturnsTrueAndProducesNewStateForMatchingActionType()
    {
        // Arrange
        DelegateActionReducer<IncrementAction, TestState> sut = new((
            state,
            _
        ) => state with
        {
            Counter = state.Counter + 1,
        });
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