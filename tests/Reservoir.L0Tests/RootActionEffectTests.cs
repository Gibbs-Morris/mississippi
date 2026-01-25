using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.L0Tests;

/// <summary>
///     Tests for <see cref="RootActionEffect{TState}" />.
/// </summary>
[AllureParentSuite("Mississippi.Reservoir")]
[AllureSuite("Core")]
[AllureSubSuite("RootActionEffect")]
public sealed class RootActionEffectTests
{
    /// <summary>
    ///     Fallback effect that handles via CanHandle but has no type index.
    /// </summary>
    private sealed class FallbackEffect : IActionEffect<TestState>
    {
        public bool WasInvoked { get; private set; }

        /// <inheritdoc />
        public bool CanHandle(
            IAction action
        ) =>
            action is SecondAction;

#pragma warning disable CS1998 // Async method lacks 'await' operators
        /// <inheritdoc />
        public async IAsyncEnumerable<IAction> HandleAsync(
            IAction action,
            TestState currentState,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            WasInvoked = true;
            yield break;
        }
#pragma warning restore CS1998
    }

    /// <summary>
    ///     Second test action for matching.
    /// </summary>
    private sealed record SecondAction : IAction;

    /// <summary>
    ///     Test action for matching.
    /// </summary>
    private sealed record TestAction(string Value) : IAction;

    /// <summary>
    ///     Test feature state.
    /// </summary>
    private sealed record TestState : IFeatureState
    {
        /// <inheritdoc />
        public static string FeatureKey => "test";
    }

    /// <summary>
    ///     Effect that throws an exception.
    /// </summary>
    private sealed class ThrowingEffect : ActionEffectBase<TestAction, TestState>
    {
        /// <inheritdoc />
        public override async IAsyncEnumerable<IAction> HandleAsync(
            TestAction action,
            TestState currentState,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            await Task.Yield();
            throw new InvalidOperationException("Test exception");
#pragma warning disable CS0162 // Unreachable code detected
            yield break;
#pragma warning restore CS0162
        }
    }

    /// <summary>
    ///     Simple effect that tracks invocation.
    /// </summary>
    private sealed class TrackingSimpleEffect : SimpleActionEffectBase<TestAction, TestState>
    {
        public bool WasInvoked { get; private set; }

        /// <inheritdoc />
        public override Task HandleAsync(
            TestAction action,
            TestState currentState,
            CancellationToken cancellationToken
        )
        {
            WasInvoked = true;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Different action that should not match.
    /// </summary>
    private sealed record UnhandledAction : IAction;

    /// <summary>
    ///     Effect that yields actions.
    /// </summary>
    private sealed class YieldingEffect : ActionEffectBase<TestAction, TestState>
    {
        /// <inheritdoc />
        public override async IAsyncEnumerable<IAction> HandleAsync(
            TestAction action,
            TestState currentState,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            await Task.Yield();
            yield return new SecondAction();
        }
    }

    /// <summary>
    ///     Constructor should throw ArgumentNullException when effects is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void ConstructorWithNullEffectsThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RootActionEffect<TestState>(null!));
    }

    /// <summary>
    ///     HandleAsync continues to other effects when one throws.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("Error Handling")]
    public async Task HandleAsyncContinuesToOtherEffectsWhenOneThrows()
    {
        // Arrange
        TrackingSimpleEffect trackingEffect = new();
        RootActionEffect<TestState> sut = new([new ThrowingEffect(), trackingEffect]);
        TestAction action = new("test");
        TestState state = new();

        // Act
        List<IAction> results = [];
        await foreach (IAction result in sut.HandleAsync(action, state, CancellationToken.None))
        {
            results.Add(result);
        }

        // Assert - tracking effect should still have been invoked
        Assert.True(trackingEffect.WasInvoked);
    }

    /// <summary>
    ///     HandleAsync dispatches to fallback effects for non-indexed types.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("Dispatch")]
    public async Task HandleAsyncDispatchesToFallbackEffects()
    {
        // Arrange
        FallbackEffect fallbackEffect = new();
        RootActionEffect<TestState> sut = new([fallbackEffect]);
        SecondAction action = new();
        TestState state = new();

        // Act
        List<IAction> results = [];
        await foreach (IAction result in sut.HandleAsync(action, state, CancellationToken.None))
        {
            results.Add(result);
        }

        // Assert
        Assert.True(fallbackEffect.WasInvoked);
    }

    /// <summary>
    ///     HandleAsync dispatches to matching effect and yields actions.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("Dispatch")]
    public async Task HandleAsyncDispatchesToMatchingEffectAndYieldsActions()
    {
        // Arrange
        RootActionEffect<TestState> sut = new([new YieldingEffect()]);
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
        Assert.IsType<SecondAction>(results[0]);
    }

    /// <summary>
    ///     HandleAsync invokes multiple effects for same action type.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("Dispatch")]
    public async Task HandleAsyncInvokesMultipleEffectsForSameActionType()
    {
        // Arrange
        TrackingSimpleEffect effect1 = new();
        TrackingSimpleEffect effect2 = new();
        RootActionEffect<TestState> sut = new([effect1, effect2]);
        TestAction action = new("test");
        TestState state = new();

        // Act
        await foreach (IAction item in sut.HandleAsync(action, state, CancellationToken.None))
        {
            _ = item; // Consume the enumerable
        }

        // Assert
        Assert.True(effect1.WasInvoked);
        Assert.True(effect2.WasInvoked);
    }

    /// <summary>
    ///     HandleAsync returns empty when no effects match.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("Dispatch")]
    public async Task HandleAsyncReturnsEmptyWhenNoEffectsMatch()
    {
        // Arrange
        RootActionEffect<TestState> sut = new([new YieldingEffect()]);
        UnhandledAction action = new();
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
    [AllureFeature("Validation")]
    public void HandleAsyncThrowsArgumentNullExceptionWhenActionIsNull()
    {
        // Arrange
        RootActionEffect<TestState> sut = new([]);
        TestState state = new();

        // Act & Assert
        // Note: The exception is thrown synchronously on method call, not during async enumeration
        IAsyncEnumerable<IAction> CallHandleAsync() => sut.HandleAsync(null!, state, CancellationToken.None);
        Assert.Throws<ArgumentNullException>(CallHandleAsync);
    }

    /// <summary>
    ///     HasEffects returns false when no effects are registered.
    /// </summary>
    [Fact]
    [AllureFeature("State")]
    public void HasEffectsReturnsFalseWhenNoEffectsRegistered()
    {
        // Arrange
        RootActionEffect<TestState> sut = new([]);

        // Assert
        Assert.False(sut.HasEffects);
    }

    /// <summary>
    ///     HasEffects returns true when effects are registered.
    /// </summary>
    [Fact]
    [AllureFeature("State")]
    public void HasEffectsReturnsTrueWhenEffectsRegistered()
    {
        // Arrange
        RootActionEffect<TestState> sut = new([new YieldingEffect()]);

        // Assert
        Assert.True(sut.HasEffects);
    }
}