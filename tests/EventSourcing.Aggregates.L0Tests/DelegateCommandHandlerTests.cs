using System;
using System.Collections.Generic;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Mississippi.EventSourcing.Aggregates.Tests;

/// <summary>
///     Tests for <see cref="DelegateCommandHandler{TCommand, TState}" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Aggregates")]
[AllureSubSuite("Command Handler")]
public class DelegateCommandHandlerTests
{
    /// <summary>
    ///     Test command record.
    /// </summary>
    /// <param name="Value">The command value.</param>
    private sealed record TestCommand(string Value);

    /// <summary>
    ///     Test event class.
    /// </summary>
    private sealed class TestEvent;

    /// <summary>
    ///     Test state record.
    /// </summary>
    /// <param name="Count">The state count.</param>
    private sealed record TestState(int Count);

    /// <summary>
    ///     Constructor should throw when handler is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenHandlerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new DelegateCommandHandler<TestCommand, TestState>(null!));
    }

    /// <summary>
    ///     Handle should invoke the delegate with the command and state.
    /// </summary>
    [Fact]
    public void HandleInvokesDelegate()
    {
        TestCommand? receivedCommand = null;
        TestState? receivedState = null;
        List<object> events = new()
        {
            new TestEvent(),
        };
        DelegateCommandHandler<TestCommand, TestState> handler = new((
            cmd,
            state
        ) =>
        {
            receivedCommand = cmd;
            receivedState = state;
            return OperationResult.Ok<IReadOnlyList<object>>(events);
        });
        TestCommand command = new("test");
        TestState state = new(42);
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);
        Assert.True(result.Success);
        Assert.Same(events, result.Value);
        Assert.Same(command, receivedCommand);
        Assert.Equal(state, receivedState);
    }

    /// <summary>
    ///     Handle should pass null state to delegate when state is null.
    /// </summary>
    [Fact]
    public void HandlePassesNullStateToDelegate()
    {
        TestState? receivedState = null;
        bool delegateCalled = false;
        DelegateCommandHandler<TestCommand, TestState> handler = new((
            _,
            state
        ) =>
        {
            delegateCalled = true;
            receivedState = state;
            return OperationResult.Ok<IReadOnlyList<object>>(Array.Empty<object>());
        });
        handler.Handle(new("test"), null);
        Assert.True(delegateCalled);
        Assert.Null(receivedState);
    }

    /// <summary>
    ///     Handle should return failed result from delegate.
    /// </summary>
    [Fact]
    public void HandleReturnsFailedResultFromDelegate()
    {
        DelegateCommandHandler<TestCommand, TestState> handler = new((
            _,
            _
        ) => OperationResult.Fail<IReadOnlyList<object>>("ERR_001", "Command failed"));
        OperationResult<IReadOnlyList<object>> result = handler.Handle(new("test"), null);
        Assert.False(result.Success);
        Assert.Equal("ERR_001", result.ErrorCode);
        Assert.Equal("Command failed", result.ErrorMessage);
    }
}