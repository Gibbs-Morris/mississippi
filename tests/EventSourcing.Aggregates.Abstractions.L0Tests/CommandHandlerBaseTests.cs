using System;
using System.Collections.Generic;


namespace Mississippi.EventSourcing.Aggregates.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="CommandHandlerBase{TCommand, TSnapshot}" /> behavior.
/// </summary>
public sealed class CommandHandlerBaseTests
{
    /// <summary>
    ///     Another command type for testing type mismatch.
    /// </summary>
    /// <param name="Id">The command ID.</param>
    private sealed record OtherCommand(int Id);

    /// <summary>
    ///     Test command record.
    /// </summary>
    /// <param name="Value">The command value.</param>
    private sealed record TestCommand(string Value);

    /// <summary>
    ///     Test handler implementation.
    /// </summary>
    private sealed class TestHandler : CommandHandlerBase<TestCommand, TestState>
    {
        /// <inheritdoc />
        protected override OperationResult<IReadOnlyList<object>> HandleCore(
            TestCommand command,
            TestState? state
        ) =>
            OperationResult.Ok<IReadOnlyList<object>>(new object[] { $"Handled: {command.Value}" });
    }

    /// <summary>
    ///     Test state record.
    /// </summary>
    /// <param name="Count">The state count.</param>
    private sealed record TestState(int Count);

    /// <summary>
    ///     Handle should call HandleCore with command and state.
    /// </summary>
    [Fact]
    public void HandleCallsHandleCore()
    {
        // Arrange
        TestHandler handler = new();
        TestCommand command = new("test-value");
        TestState state = new(42);

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Value!);
        Assert.Equal("Handled: test-value", result.Value[0]);
    }

    /// <summary>
    ///     Handle should throw ArgumentNullException when command is null.
    /// </summary>
    [Fact]
    public void HandleThrowsWhenCommandIsNull()
    {
        // Arrange
        TestHandler handler = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => handler.Handle(null!, null));
    }

    /// <summary>
    ///     Handle should work with null state.
    /// </summary>
    [Fact]
    public void HandleWorksWithNullState()
    {
        // Arrange
        TestHandler handler = new();
        TestCommand command = new("no-state");

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Value!);
        Assert.Equal("Handled: no-state", result.Value[0]);
    }

    /// <summary>
    ///     TryHandle should return false for non-matching command type.
    /// </summary>
    [Fact]
    public void TryHandleReturnsFalseForNonMatchingType()
    {
        // Arrange
        TestHandler handler = new();
        OtherCommand command = new(123);

        // Act
        bool handled = handler.TryHandle(command, null, out OperationResult<IReadOnlyList<object>> _);

        // Assert
        Assert.False(handled);
    }

    /// <summary>
    ///     TryHandle should return true and handle matching command type.
    /// </summary>
    [Fact]
    public void TryHandleReturnsTrueForMatchingType()
    {
        // Arrange
        TestHandler handler = new();
        TestCommand command = new("matching");

        // Act
        bool handled = handler.TryHandle(command, null, out OperationResult<IReadOnlyList<object>> result);

        // Assert
        Assert.True(handled);
        Assert.True(result.Success);
        Assert.Equal("Handled: matching", result.Value![0]);
    }

    /// <summary>
    ///     TryHandle should throw ArgumentNullException when command is null.
    /// </summary>
    [Fact]
    public void TryHandleThrowsWhenCommandIsNull()
    {
        // Arrange
        TestHandler handler = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => handler.TryHandle(
            null!,
            null,
            out OperationResult<IReadOnlyList<object>> _));
    }
}