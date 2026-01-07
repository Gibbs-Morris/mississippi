using System;

using Allure.Xunit.Attributes;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.L0Tests;

/// <summary>
///     Tests for <see cref="RootReducer{TState}" />.
/// </summary>
[AllureParentSuite("Mississippi.Reservoir")]
[AllureSuite("Core")]
[AllureSubSuite("RootReducer")]
public sealed class RootReducerTests
{
    /// <summary>
    ///     Test action for unit tests.
    /// </summary>
    private sealed record IncrementAction : IAction;

    /// <summary>
    ///     Test reducer that increments counter.
    /// </summary>
    private sealed class IncrementReducer : Reducer<IncrementAction, TestState>
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
    ///     Test action for unit tests.
    /// </summary>
    private sealed record UnknownAction : IAction;

    /// <summary>
    ///     Constructor should throw ArgumentNullException when reducers is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void ConstructorWithNullReducersThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RootReducer<TestState>(null!));
    }

    /// <summary>
    ///     RootReducer should apply matching reducer.
    /// </summary>
    [Fact]
    [AllureFeature("Reduction")]
    public void ReduceAppliesMatchingReducer()
    {
        // Arrange
        RootReducer<TestState> sut = new([new IncrementReducer()]);
        TestState initialState = new()
        {
            Counter = 5,
        };

        // Act
        TestState result = sut.Reduce(initialState, new IncrementAction());

        // Assert
        Assert.Equal(6, result.Counter);
    }

    /// <summary>
    ///     RootReducer should apply multiple matching reducers in order.
    /// </summary>
    [Fact]
    [AllureFeature("Reduction")]
    public void ReduceAppliesMultipleReducersInOrder()
    {
        // Arrange
        RootReducer<TestState> sut = new([new IncrementReducer(), new IncrementReducer()]);
        TestState initialState = new()
        {
            Counter = 0,
        };

        // Act
        TestState result = sut.Reduce(initialState, new IncrementAction());

        // Assert
        Assert.Equal(2, result.Counter);
    }

    /// <summary>
    ///     RootReducer should return same state when no reducers match.
    /// </summary>
    [Fact]
    [AllureFeature("Reduction")]
    public void ReduceReturnsOriginalStateWhenNoReducersMatch()
    {
        // Arrange
        RootReducer<TestState> sut = new([]);
        TestState initialState = new()
        {
            Counter = 5,
        };

        // Act
        TestState result = sut.Reduce(initialState, new UnknownAction());

        // Assert
        Assert.Same(initialState, result);
    }
}