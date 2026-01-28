using System;
using System.Collections.Immutable;

using Mississippi.Inlet.Client.Abstractions.Commands;


namespace Mississippi.Inlet.Client.L0Tests;

/// <summary>
///     Tests for AggregateCommandStateBase.
/// </summary>
public sealed class AggregateCommandStateBaseTests
{
    /// <summary>
    ///     CommandHistory can be set via init.
    /// </summary>
    [Fact]
    public void CommandHistoryCanBeSet()
    {
        // Arrange
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            DateTimeOffset.UtcNow);
        ImmutableList<CommandHistoryEntry> history = ImmutableList.Create(entry);

        // Act
        ConcreteAggregateState state = new()
        {
            CommandHistory = history,
        };

        // Assert
        Assert.Single(state.CommandHistory);
    }

    /// <summary>
    ///     Default CommandHistory is empty.
    /// </summary>
    [Fact]
    public void DefaultCommandHistoryIsEmpty()
    {
        // Act
        ConcreteAggregateState state = new();

        // Assert
        Assert.Empty(state.CommandHistory);
    }

    /// <summary>
    ///     Default ErrorCode is null.
    /// </summary>
    [Fact]
    public void DefaultErrorCodeIsNull()
    {
        // Act
        ConcreteAggregateState state = new();

        // Assert
        Assert.Null(state.ErrorCode);
    }

    /// <summary>
    ///     Default ErrorMessage is null.
    /// </summary>
    [Fact]
    public void DefaultErrorMessageIsNull()
    {
        // Act
        ConcreteAggregateState state = new();

        // Assert
        Assert.Null(state.ErrorMessage);
    }

    /// <summary>
    ///     Default InFlightCommands is empty.
    /// </summary>
    [Fact]
    public void DefaultInFlightCommandsIsEmpty()
    {
        // Act
        ConcreteAggregateState state = new();

        // Assert
        Assert.Empty(state.InFlightCommands);
    }

    /// <summary>
    ///     Default LastCommandSucceeded is null.
    /// </summary>
    [Fact]
    public void DefaultLastCommandSucceededIsNull()
    {
        // Act
        ConcreteAggregateState state = new();

        // Assert
        Assert.Null(state.LastCommandSucceeded);
    }

    /// <summary>
    ///     ErrorCode can be set via init.
    /// </summary>
    [Fact]
    public void ErrorCodeCanBeSet()
    {
        // Act
        ConcreteAggregateState state = new()
        {
            ErrorCode = "ERR001",
        };

        // Assert
        Assert.Equal("ERR001", state.ErrorCode);
    }

    /// <summary>
    ///     ErrorMessage can be set via init.
    /// </summary>
    [Fact]
    public void ErrorMessageCanBeSet()
    {
        // Act
        ConcreteAggregateState state = new()
        {
            ErrorMessage = "Something went wrong",
        };

        // Assert
        Assert.Equal("Something went wrong", state.ErrorMessage);
    }

    /// <summary>
    ///     InFlightCommands can be set via init.
    /// </summary>
    [Fact]
    public void InFlightCommandsCanBeSet()
    {
        // Act
        ConcreteAggregateState state = new()
        {
            InFlightCommands = ImmutableHashSet.Create("cmd-1", "cmd-2"),
        };

        // Assert
        Assert.Equal(2, state.InFlightCommands.Count);
    }

    /// <summary>
    ///     IsExecuting returns false when InFlightCommands is empty.
    /// </summary>
    [Fact]
    public void IsExecutingReturnsFalseWhenEmpty()
    {
        // Act
        ConcreteAggregateState state = new();

        // Assert
        Assert.False(state.IsExecuting);
    }

    /// <summary>
    ///     IsExecuting returns true when InFlightCommands is not empty.
    /// </summary>
    [Fact]
    public void IsExecutingReturnsTrueWhenNotEmpty()
    {
        // Arrange
        ConcreteAggregateState state = new()
        {
            InFlightCommands = ImmutableHashSet.Create("cmd-123"),
        };

        // Assert
        Assert.True(state.IsExecuting);
    }

    /// <summary>
    ///     LastCommandSucceeded can be set to false.
    /// </summary>
    [Fact]
    public void LastCommandSucceededCanBeSetToFalse()
    {
        // Act
        ConcreteAggregateState state = new()
        {
            LastCommandSucceeded = false,
        };

        // Assert
        Assert.False(state.LastCommandSucceeded);
    }

    /// <summary>
    ///     LastCommandSucceeded can be set to true.
    /// </summary>
    [Fact]
    public void LastCommandSucceededCanBeSetToTrue()
    {
        // Act
        ConcreteAggregateState state = new()
        {
            LastCommandSucceeded = true,
        };

        // Assert
        Assert.True(state.LastCommandSucceeded);
    }
}