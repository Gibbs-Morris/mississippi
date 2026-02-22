using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.L0Tests;

/// <summary>
///     Tests for <see cref="SimpleActionEffectBase{TAction,TState}" />.
/// </summary>
public sealed class SimpleActionEffectBaseTests
{
    /// <summary>
    ///     Different action that should not match.
    /// </summary>
    private sealed record OtherAction : IAction;

    /// <summary>
    ///     Test action for matching.
    /// </summary>
    private sealed record TestAction(string Value) : IAction;

    /// <summary>
    ///     Concrete test effect implementation.
    /// </summary>
    private sealed class TestSimpleEffect : SimpleActionEffectBase<TestAction, TestState>
    {
        public TestAction? HandledAction { get; private set; }

        public bool WasHandled { get; private set; }

        /// <inheritdoc />
        public override Task HandleAsync(
            TestAction action,
            TestState currentState,
            CancellationToken cancellationToken
        )
        {
            WasHandled = true;
            HandledAction = action;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Test feature state.
    /// </summary>
    private sealed record TestState : IFeatureState
    {
        /// <inheritdoc />
        public static string FeatureKey => "test";
    }

    /// <summary>
    ///     CanHandle returns false for non-matching action type.
    /// </summary>
    [Fact]
    public void CanHandleReturnsFalseForNonMatchingActionType()
    {
        // Arrange
        TestSimpleEffect sut = new();
        OtherAction action = new();

        // Act
        bool result = sut.CanHandle(action);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    ///     CanHandle returns true for matching action type.
    /// </summary>
    [Fact]
    public void CanHandleReturnsTrueForMatchingActionType()
    {
        // Arrange
        TestSimpleEffect sut = new();
        TestAction action = new("test");

        // Act
        bool result = sut.CanHandle(action);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    ///     CanHandle throws ArgumentNullException when action is null.
    /// </summary>
    [Fact]
    public void CanHandleThrowsArgumentNullExceptionWhenActionIsNull()
    {
        // Arrange
        TestSimpleEffect sut = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.CanHandle(null!));
    }

    /// <summary>
    ///     HandleAsync invokes simple handler and yields no actions.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleAsyncInvokesSimpleHandlerAndYieldsNoActions()
    {
        // Arrange
        TestSimpleEffect sut = new();
        IActionEffect<TestState> effect = sut;
        TestAction action = new("test-value");
        TestState state = new();

        // Act
        List<IAction> results = [];
        await foreach (IAction result in effect.HandleAsync(action, state, CancellationToken.None))
        {
            results.Add(result);
        }

        // Assert
        Assert.Empty(results);
        Assert.True(sut.WasHandled);
        Assert.Equal(action, sut.HandledAction);
    }

    /// <summary>
    ///     HandleAsync returns empty enumerable for non-matching action.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleAsyncReturnsEmptyForNonMatchingAction()
    {
        // Arrange
        TestSimpleEffect sut = new();
        IActionEffect<TestState> effect = sut;
        OtherAction action = new();
        TestState state = new();

        // Act
        List<IAction> results = [];
        await foreach (IAction result in effect.HandleAsync(action, state, CancellationToken.None))
        {
            results.Add(result);
        }

        // Assert
        Assert.Empty(results);
        Assert.False(sut.WasHandled);
    }

    /// <summary>
    ///     HandleAsync throws ArgumentNullException when action is null.
    /// </summary>
    [Fact]
    public void HandleAsyncThrowsArgumentNullExceptionWhenActionIsNull()
    {
        // Arrange
        TestSimpleEffect sut = new();
        IActionEffect<TestState> effect = sut;
        TestState state = new();

        // Act & Assert
        // Note: The exception is thrown synchronously on method call, not during async enumeration
        IAsyncEnumerable<IAction> CallHandleAsync() => effect.HandleAsync(null!, state, CancellationToken.None);
        Assert.Throws<ArgumentNullException>(CallHandleAsync);
    }
}