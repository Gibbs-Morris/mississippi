using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Testing.L0Tests;

/// <summary>
///     Tests for <see cref="StoreTestHarness{TState}" /> and <see cref="StoreScenario{TState}" />.
/// </summary>
public sealed class StoreTestHarnessTests
{
    private sealed record IncrementAction : IAction;

    private sealed record SetValueAction(string Value) : IAction;

    private sealed class TestEffect : ActionEffectBase<SetValueAction, TestState>
    {
        public override async IAsyncEnumerable<IAction> HandleAsync(
            SetValueAction action,
            TestState currentState,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            await Task.CompletedTask;
            yield return new ValueSetNotification(action.Value);
        }
    }

    private sealed record TestState : IFeatureState
    {
        public static string FeatureKey => "test";

        public int Counter { get; init; }

        public string? Value { get; init; }
    }

    private sealed record ValueSetNotification(string Value) : IAction;

    /// <summary>
    ///     Verifies that CreateScenario returns a scenario with the initial state.
    /// </summary>
    [Fact]
    public void CreateScenarioReturnsScenarioWithInitialState()
    {
        // Arrange
        StoreTestHarness<TestState> harness = StoreTestHarnessFactory.ForFeature<TestState>()
            .WithInitialState(
                new()
                {
                    Value = "initial",
                });

        // Act
        StoreScenario<TestState> scenario = harness.CreateScenario();

        // Assert
        scenario.State.Value.Should().Be("initial");
    }

    /// <summary>
    ///     Verifies that chaining Given, When, and Then methods works correctly.
    /// </summary>
    [Fact]
    public void FluentChainingWorksCorrectly()
    {
        // Arrange
        StoreTestHarness<TestState> harness = StoreTestHarnessFactory.ForFeature<TestState>()
            .WithReducer<SetValueAction>((
                state,
                action
            ) => state with
            {
                Value = action.Value,
            })
            .WithReducer<IncrementAction>((
                state,
                _
            ) => state with
            {
                Counter = state.Counter + 1,
            });

        // Act & Assert
        harness.CreateScenario()
            .Given(new SetValueAction("initial"))
            .When(new IncrementAction())
            .ThenState(s =>
            {
                s.Value.Should().Be("initial");
                s.Counter.Should().Be(1);
            })
            .ThenEmitsNothing();
    }

    /// <summary>
    ///     Verifies that Given applies actions through reducers.
    /// </summary>
    [Fact]
    public void GivenAppliesActionsThruReducers()
    {
        // Arrange
        StoreTestHarness<TestState> harness = StoreTestHarnessFactory.ForFeature<TestState>()
            .WithReducer<SetValueAction>((
                state,
                action
            ) => state with
            {
                Value = action.Value,
            });

        // Act
        StoreScenario<TestState> scenario = harness.CreateScenario()
            .Given(new SetValueAction("first"), new SetValueAction("second"));

        // Assert
        scenario.State.Value.Should().Be("second");
    }

    /// <summary>
    ///     Verifies that GivenState sets state directly.
    /// </summary>
    [Fact]
    public void GivenStateSetsStateDirectly()
    {
        // Arrange
        StoreTestHarness<TestState> harness = StoreTestHarnessFactory.ForFeature<TestState>();

        // Act
        StoreScenario<TestState> scenario = harness.CreateScenario()
            .GivenState(
                new()
                {
                    Value = "direct",
                });

        // Assert
        scenario.State.Value.Should().Be("direct");
    }

    /// <summary>
    ///     Verifies that ThenEmitsNothing succeeds when no actions are emitted.
    /// </summary>
    [Fact]
    public void ThenEmitsNothingSucceedsWhenNoActionsEmitted()
    {
        // Arrange
        StoreTestHarness<TestState> harness = StoreTestHarnessFactory.ForFeature<TestState>()
            .WithReducer<SetValueAction>((
                state,
                action
            ) => state with
            {
                Value = action.Value,
            });

        // Act
        StoreScenario<TestState> scenario = harness.CreateScenario()
            .When(new SetValueAction("test"))
            .ThenEmitsNothing();

        // Assert
        scenario.EmittedActions.Should().BeEmpty();
    }

    /// <summary>
    ///     Verifies that ThenEmitsNothing throws when actions are emitted.
    /// </summary>
    [Fact]
    public void ThenEmitsNothingThrowsWhenActionsEmitted()
    {
        // Arrange
        StoreTestHarness<TestState> harness = StoreTestHarnessFactory.ForFeature<TestState>()
            .WithEffect(new TestEffect());

        // Act
        StoreScenario<TestState> scenario = harness.CreateScenario().When(new SetValueAction("test"));

        // Assert
        Action act = () => scenario.ThenEmitsNothing();
        act.Should().Throw<InvalidOperationException>().WithMessage("*Expected no actions*ValueSetNotification*");
    }

    /// <summary>
    ///     Verifies that ThenEmits succeeds when expected action is emitted.
    /// </summary>
    [Fact]
    public void ThenEmitsSucceedsWhenActionEmitted()
    {
        // Arrange
        StoreTestHarness<TestState> harness = StoreTestHarnessFactory.ForFeature<TestState>()
            .WithEffect(new TestEffect());

        // Act & Assert - should not throw
        harness.CreateScenario()
            .When(new SetValueAction("test"))
            .ThenEmits<ValueSetNotification>(n => n.Value.Should().Be("test"));
    }

    /// <summary>
    ///     Verifies that ThenEmits throws when expected action is not emitted.
    /// </summary>
    [Fact]
    public void ThenEmitsThrowsWhenActionNotEmitted()
    {
        // Arrange
        StoreTestHarness<TestState> harness = StoreTestHarnessFactory.ForFeature<TestState>();

        // Act
        StoreScenario<TestState> scenario = harness.CreateScenario().When(new SetValueAction("test"));

        // Assert
        Action act = () => scenario.ThenEmits<ValueSetNotification>();
        act.Should().Throw<InvalidOperationException>().WithMessage("*Expected action*ValueSetNotification*");
    }

    /// <summary>
    ///     Verifies that ThenState runs assertion on current state.
    /// </summary>
    [Fact]
    public void ThenStateRunsAssertionOnCurrentState()
    {
        // Arrange
        StoreTestHarness<TestState> harness = StoreTestHarnessFactory.ForFeature<TestState>()
            .WithReducer<SetValueAction>((
                state,
                action
            ) => state with
            {
                Value = action.Value,
            });

        // Act & Assert - should not throw
        harness.CreateScenario().When(new SetValueAction("expected")).ThenState(s => s.Value.Should().Be("expected"));
    }

    /// <summary>
    ///     Verifies that When applies action through reducers and runs effects.
    /// </summary>
    [Fact]
    public void WhenAppliesReducersAndRunsEffects()
    {
        // Arrange
        StoreTestHarness<TestState> harness = StoreTestHarnessFactory.ForFeature<TestState>()
            .WithReducer<SetValueAction>((
                state,
                action
            ) => state with
            {
                Value = action.Value,
            })
            .WithEffect(new TestEffect());

        // Act
        StoreScenario<TestState> scenario = harness.CreateScenario().When(new SetValueAction("test"));

        // Assert
        scenario.State.Value.Should().Be("test");
        scenario.EmittedActions.Should().ContainSingle().Which.Should().BeOfType<ValueSetNotification>();
    }
}