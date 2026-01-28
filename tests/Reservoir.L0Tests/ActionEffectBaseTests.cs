using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;


using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.L0Tests;

/// <summary>
///     Tests for <see cref="ActionEffectBase{TAction,TState}" />.
/// </summary>
public sealed class ActionEffectBaseTests
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
    private sealed class TestEffect : ActionEffectBase<TestAction, TestState>
    {
        /// <inheritdoc />
        public override async IAsyncEnumerable<IAction> HandleAsync(
            TestAction action,
            TestState currentState,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            await Task.Yield();
            yield return new OtherAction();
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
        TestEffect sut = new();
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
        TestEffect sut = new();
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
        TestEffect sut = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.CanHandle(null!));
    }

    /// <summary>
    ///     HandleAsync dispatches to typed method for matching action.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
        public async Task HandleAsyncDispatchesToTypedMethodForMatchingAction()
    {
        // Arrange
        TestEffect sut = new();
        TestAction action = new("test");
        TestState state = new();

        // Act
        List<IAction> results = [];
        await foreach (IAction result in sut.HandleAsync(action, state, CancellationToken.None))
        {
            results.Add(result);
        }

        // Assert
        Assert.Single(results);
        Assert.IsType<OtherAction>(results[0]);
    }

    /// <summary>
    ///     HandleAsync returns empty enumerable for non-matching action.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
        public async Task HandleAsyncReturnsEmptyForNonMatchingAction()
    {
        // Arrange
        TestEffect sut = new();
        OtherAction action = new();
        TestState state = new();

        // Act
        List<IAction> results = [];
        await foreach (IAction result in sut.HandleAsync(action, state, CancellationToken.None))
        {
            results.Add(result);
        }

        // Assert
        Assert.Empty(results);
    }

    /// <summary>
    ///     HandleAsync throws ArgumentNullException when action is null.
    /// </summary>
    [Fact]
        public void HandleAsyncThrowsArgumentNullExceptionWhenActionIsNull()
    {
        // Arrange
        TestEffect sut = new();
        IActionEffect<TestState> effect = sut;
        TestState state = new();

        // Act & Assert
        // Note: The exception is thrown synchronously on method call, not during async enumeration
        IAsyncEnumerable<IAction> CallHandleAsync() => effect.HandleAsync(null!, state, CancellationToken.None);
        Assert.Throws<ArgumentNullException>(CallHandleAsync);
    }
}