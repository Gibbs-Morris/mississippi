using System;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.L0Tests;

/// <summary>
///     Tests for <see cref="RootReducer{TState}" />.
/// </summary>
public sealed class RootReducerTests
{
    /// <summary>
    ///     A fallback reducer that increments for any action.
    /// </summary>
    private sealed class FallbackActionReducer : IActionReducer<TestState>
    {
        /// <inheritdoc />
        public bool TryReduce(
            TestState state,
            IAction action,
            out TestState newState
        )
        {
            newState = state with
            {
                Counter = state.Counter + 1,
            };
            return true;
        }
    }

    /// <summary>
    ///     Test action for unit tests.
    /// </summary>
    private sealed record IncrementAction : IAction;

    /// <summary>
    ///     Test reducer that increments counter.
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
    public void ConstructorWithNullReducersThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RootReducer<TestState>(null!));
    }

    /// <summary>
    ///     RootReducer should apply both indexed and fallback reducers.
    /// </summary>
    [Fact]
    public void ReduceAppliesBothIndexedAndFallbackReducers()
    {
        // Arrange
        RootReducer<TestState> sut = new([new IncrementActionReducer(), new FallbackActionReducer()]);
        TestState initialState = new()
        {
            Counter = 0,
        };

        // Act
        TestState result = sut.Reduce(initialState, new IncrementAction());

        // Assert - should increment twice (once by indexed, once by fallback)
        Assert.Equal(2, result.Counter);
    }

    /// <summary>
    ///     RootReducer should apply matching reducer.
    /// </summary>
    [Fact]
    public void ReduceAppliesMatchingReducer()
    {
        // Arrange
        RootReducer<TestState> sut = new([new IncrementActionReducer()]);
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
    public void ReduceAppliesMultipleReducersInOrder()
    {
        // Arrange
        RootReducer<TestState> sut = new([new IncrementActionReducer(), new IncrementActionReducer()]);
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
    ///     RootReducer should handle fallback reducers (IActionReducer without specific TAction).
    /// </summary>
    [Fact]
    public void ReduceHandlesFallbackReducers()
    {
        // Arrange
        RootReducer<TestState> sut = new([new FallbackActionReducer()]);
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
    ///     RootReducer should return same state when no reducers match.
    /// </summary>
    [Fact]
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

    /// <summary>
    ///     Reduce should throw ArgumentNullException when action is null.
    /// </summary>
    [Fact]
    public void ReduceWithNullActionThrowsArgumentNullException()
    {
        // Arrange
        RootReducer<TestState> sut = new([]);
        TestState initialState = new()
        {
            Counter = 5,
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.Reduce(initialState, null!));
    }
}