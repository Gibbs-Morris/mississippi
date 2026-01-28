using System;
using System.Collections.Immutable;


using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.Abstractions.Commands;


namespace Mississippi.Inlet.Client.L0Tests;

/// <summary>
///     Tests for AggregateCommandStateReducers.
/// </summary>
public sealed class AggregateCommandStateReducersTests
{
    /// <summary>
    ///     ComputeCommandExecuting adds entry to history.
    /// </summary>
    [Fact]
        public void ComputeCommandExecutingAddsEntryToHistory()
    {
        // Arrange
        TestAggregateState state = new();
        TestCommandExecutingAction action = new("cmd-123", "TestCommand", DateTimeOffset.UtcNow);

        // Act
        (ImmutableHashSet<string> _, ImmutableList<CommandHistoryEntry> history) =
            AggregateCommandStateReducers.ComputeCommandExecuting(state, action);

        // Assert
        Assert.Single(history);
        Assert.Equal("cmd-123", history[0].CommandId);
    }

    /// <summary>
    ///     ComputeCommandExecuting adds command to in-flight set.
    /// </summary>
    [Fact]
        public void ComputeCommandExecutingAddsToInFlightSet()
    {
        // Arrange
        TestAggregateState state = new();
        TestCommandExecutingAction action = new("cmd-123", "TestCommand", DateTimeOffset.UtcNow);

        // Act
        (ImmutableHashSet<string> inFlight, ImmutableList<CommandHistoryEntry> _) =
            AggregateCommandStateReducers.ComputeCommandExecuting(state, action);

        // Assert
        Assert.Contains("cmd-123", inFlight);
    }

    /// <summary>
    ///     ComputeCommandExecuting creates history entry with Executing status.
    /// </summary>
    [Fact]
        public void ComputeCommandExecutingCreatesEntryWithExecutingStatus()
    {
        // Arrange
        TestAggregateState state = new();
        TestCommandExecutingAction action = new("cmd-123", "TestCommand", DateTimeOffset.UtcNow);

        // Act
        (ImmutableHashSet<string> _, ImmutableList<CommandHistoryEntry> history) =
            AggregateCommandStateReducers.ComputeCommandExecuting(state, action);

        // Assert
        Assert.Equal(CommandStatus.Executing, history[0].Status);
    }

    /// <summary>
    ///     ComputeCommandExecuting enforces max history entries.
    /// </summary>
    [Fact]
        public void ComputeCommandExecutingEnforcesMaxHistoryEntries()
    {
        // Arrange
        ImmutableList<CommandHistoryEntry>.Builder historyBuilder = ImmutableList.CreateBuilder<CommandHistoryEntry>();
        for (int i = 0; i < 5; i++)
        {
            historyBuilder.Add(CommandHistoryEntry.CreateExecuting($"old-{i}", "OldCommand", DateTimeOffset.UtcNow));
        }

        TestAggregateState state = new()
        {
            CommandHistory = historyBuilder.ToImmutable(),
        };
        TestCommandExecutingAction action = new("new-cmd", "TestCommand", DateTimeOffset.UtcNow);

        // Act
        (ImmutableHashSet<string> _, ImmutableList<CommandHistoryEntry> history) =
            AggregateCommandStateReducers.ComputeCommandExecuting(state, action, 3);

        // Assert
        Assert.Equal(3, history.Count);
        Assert.Equal("new-cmd", history[^1].CommandId);
    }

    /// <summary>
    ///     ComputeCommandExecuting throws on null action.
    /// </summary>
    [Fact]
        public void ComputeCommandExecutingThrowsOnNullAction()
    {
        // Arrange
        TestAggregateState state = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => AggregateCommandStateReducers.ComputeCommandExecuting(state, null!));
    }

    /// <summary>
    ///     ComputeCommandExecuting throws on null state.
    /// </summary>
    [Fact]
        public void ComputeCommandExecutingThrowsOnNullState()
    {
        // Arrange
        TestCommandExecutingAction action = new("cmd-123", "TestCommand", DateTimeOffset.UtcNow);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            AggregateCommandStateReducers.ComputeCommandExecuting(null!, action));
    }

    /// <summary>
    ///     ComputeCommandFailed removes command from in-flight set.
    /// </summary>
    [Fact]
        public void ComputeCommandFailedRemovesFromInFlightSet()
    {
        // Arrange
        TestAggregateState state = new()
        {
            InFlightCommands = ImmutableHashSet.Create("cmd-123"),
        };
        TestCommandFailedAction action = new("cmd-123", DateTimeOffset.UtcNow, "ERR001", "Failed");

        // Act
        (ImmutableHashSet<string> inFlight, ImmutableList<CommandHistoryEntry> _) =
            AggregateCommandStateReducers.ComputeCommandFailed(state, action);

        // Assert
        Assert.DoesNotContain("cmd-123", inFlight);
    }

    /// <summary>
    ///     ComputeCommandFailed throws on null action.
    /// </summary>
    [Fact]
        public void ComputeCommandFailedThrowsOnNullAction()
    {
        // Arrange
        TestAggregateState state = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => AggregateCommandStateReducers.ComputeCommandFailed(state, null!));
    }

    /// <summary>
    ///     ComputeCommandFailed throws on null state.
    /// </summary>
    [Fact]
        public void ComputeCommandFailedThrowsOnNullState()
    {
        // Arrange
        TestCommandFailedAction action = new("cmd-123", DateTimeOffset.UtcNow, "ERR", "Error");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => AggregateCommandStateReducers.ComputeCommandFailed(null!, action));
    }

    /// <summary>
    ///     ComputeCommandFailed updates history entry to Failed.
    /// </summary>
    [Fact]
        public void ComputeCommandFailedUpdatesHistoryEntryToFailed()
    {
        // Arrange
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            DateTimeOffset.UtcNow);
        TestAggregateState state = new()
        {
            InFlightCommands = ImmutableHashSet.Create("cmd-123"),
            CommandHistory = ImmutableList.Create(entry),
        };
        TestCommandFailedAction action = new("cmd-123", DateTimeOffset.UtcNow.AddSeconds(5), "ERR001", "Failed");

        // Act
        (ImmutableHashSet<string> _, ImmutableList<CommandHistoryEntry> history) =
            AggregateCommandStateReducers.ComputeCommandFailed(state, action);

        // Assert
        Assert.Single(history);
        Assert.Equal(CommandStatus.Failed, history[0].Status);
    }

    /// <summary>
    ///     ComputeCommandSucceeded removes command from in-flight set.
    /// </summary>
    [Fact]
        public void ComputeCommandSucceededRemovesFromInFlightSet()
    {
        // Arrange
        TestAggregateState state = new()
        {
            InFlightCommands = ImmutableHashSet.Create("cmd-123"),
        };
        TestCommandSucceededAction action = new("cmd-123", DateTimeOffset.UtcNow);

        // Act
        (ImmutableHashSet<string> inFlight, ImmutableList<CommandHistoryEntry> _) =
            AggregateCommandStateReducers.ComputeCommandSucceeded(state, action);

        // Assert
        Assert.DoesNotContain("cmd-123", inFlight);
    }

    /// <summary>
    ///     ComputeCommandSucceeded throws on null action.
    /// </summary>
    [Fact]
        public void ComputeCommandSucceededThrowsOnNullAction()
    {
        // Arrange
        TestAggregateState state = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => AggregateCommandStateReducers.ComputeCommandSucceeded(state, null!));
    }

    /// <summary>
    ///     ComputeCommandSucceeded throws on null state.
    /// </summary>
    [Fact]
        public void ComputeCommandSucceededThrowsOnNullState()
    {
        // Arrange
        TestCommandSucceededAction action = new("cmd-123", DateTimeOffset.UtcNow);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            AggregateCommandStateReducers.ComputeCommandSucceeded(null!, action));
    }

    /// <summary>
    ///     ComputeCommandSucceeded updates history entry to Succeeded.
    /// </summary>
    [Fact]
        public void ComputeCommandSucceededUpdatesHistoryEntryToSucceeded()
    {
        // Arrange
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            DateTimeOffset.UtcNow);
        TestAggregateState state = new()
        {
            InFlightCommands = ImmutableHashSet.Create("cmd-123"),
            CommandHistory = ImmutableList.Create(entry),
        };
        TestCommandSucceededAction action = new("cmd-123", DateTimeOffset.UtcNow.AddSeconds(5));

        // Act
        (ImmutableHashSet<string> _, ImmutableList<CommandHistoryEntry> history) =
            AggregateCommandStateReducers.ComputeCommandSucceeded(state, action);

        // Assert
        Assert.Single(history);
        Assert.Equal(CommandStatus.Succeeded, history[0].Status);
    }

    /// <summary>
    ///     ReduceCommandExecuting adds command to in-flight set.
    /// </summary>
    [Fact]
        public void ReduceCommandExecutingAddsToInFlightSet()
    {
        // Arrange
        TestAggregateState state = new();
        TestCommandExecutingAction action = new("cmd-456", "TestCommand", DateTimeOffset.UtcNow);

        // Act
        TestAggregateState result = AggregateCommandStateReducers.ReduceCommandExecuting(state, action);

        // Assert
        Assert.Contains("cmd-456", result.InFlightCommands);
    }

    /// <summary>
    ///     ReduceCommandExecuting clears error state.
    /// </summary>
    [Fact]
        public void ReduceCommandExecutingClearsErrorState()
    {
        // Arrange
        TestAggregateState state = new()
        {
            ErrorCode = "PREV_ERR",
            ErrorMessage = "Previous error",
        };
        TestCommandExecutingAction action = new("cmd-123", "TestCommand", DateTimeOffset.UtcNow);

        // Act
        TestAggregateState result = AggregateCommandStateReducers.ReduceCommandExecuting(state, action);

        // Assert
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    /// <summary>
    ///     ReduceCommandExecuting clears LastCommandSucceeded.
    /// </summary>
    [Fact]
        public void ReduceCommandExecutingClearsLastCommandSucceeded()
    {
        // Arrange
        TestAggregateState state = new()
        {
            LastCommandSucceeded = true,
        };
        TestCommandExecutingAction action = new("cmd-123", "TestCommand", DateTimeOffset.UtcNow);

        // Act
        TestAggregateState result = AggregateCommandStateReducers.ReduceCommandExecuting(state, action);

        // Assert
        Assert.Null(result.LastCommandSucceeded);
    }

    /// <summary>
    ///     ReduceCommandExecuting throws on null state.
    /// </summary>
    [Fact]
        public void ReduceCommandExecutingThrowsOnNullState()
    {
        // Arrange
        TestCommandExecutingAction action = new("cmd-123", "TestCommand", DateTimeOffset.UtcNow);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            AggregateCommandStateReducers.ReduceCommandExecuting<TestAggregateState>(null!, action));
    }

    /// <summary>
    ///     ReduceCommandFailed sets error state.
    /// </summary>
    [Fact]
        public void ReduceCommandFailedSetsErrorState()
    {
        // Arrange
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            DateTimeOffset.UtcNow);
        TestAggregateState state = new()
        {
            InFlightCommands = ImmutableHashSet.Create("cmd-123"),
            CommandHistory = ImmutableList.Create(entry),
        };
        TestCommandFailedAction action = new("cmd-123", DateTimeOffset.UtcNow, "INSUFFICIENT_FUNDS", "Not enough");

        // Act
        TestAggregateState result = AggregateCommandStateReducers.ReduceCommandFailed(state, action);

        // Assert
        Assert.Equal("INSUFFICIENT_FUNDS", result.ErrorCode);
        Assert.Equal("Not enough", result.ErrorMessage);
    }

    /// <summary>
    ///     ReduceCommandFailed sets LastCommandSucceeded to false.
    /// </summary>
    [Fact]
        public void ReduceCommandFailedSetsLastCommandSucceededToFalse()
    {
        // Arrange
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            DateTimeOffset.UtcNow);
        TestAggregateState state = new()
        {
            InFlightCommands = ImmutableHashSet.Create("cmd-123"),
            CommandHistory = ImmutableList.Create(entry),
        };
        TestCommandFailedAction action = new("cmd-123", DateTimeOffset.UtcNow, "ERR", "Error");

        // Act
        TestAggregateState result = AggregateCommandStateReducers.ReduceCommandFailed(state, action);

        // Assert
        Assert.False(result.LastCommandSucceeded);
    }

    /// <summary>
    ///     ReduceCommandFailed throws on null state.
    /// </summary>
    [Fact]
        public void ReduceCommandFailedThrowsOnNullState()
    {
        // Arrange
        TestCommandFailedAction action = new("cmd-123", DateTimeOffset.UtcNow, "ERR", "Error");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            AggregateCommandStateReducers.ReduceCommandFailed<TestAggregateState>(null!, action));
    }

    /// <summary>
    ///     ReduceCommandSucceeded clears error state.
    /// </summary>
    [Fact]
        public void ReduceCommandSucceededClearsErrorState()
    {
        // Arrange
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            DateTimeOffset.UtcNow);
        TestAggregateState state = new()
        {
            InFlightCommands = ImmutableHashSet.Create("cmd-123"),
            CommandHistory = ImmutableList.Create(entry),
            ErrorCode = "PREV_ERR",
            ErrorMessage = "Previous error",
        };
        TestCommandSucceededAction action = new("cmd-123", DateTimeOffset.UtcNow);

        // Act
        TestAggregateState result = AggregateCommandStateReducers.ReduceCommandSucceeded(state, action);

        // Assert
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    /// <summary>
    ///     ReduceCommandSucceeded sets LastCommandSucceeded to true.
    /// </summary>
    [Fact]
        public void ReduceCommandSucceededSetsLastCommandSucceededToTrue()
    {
        // Arrange
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            DateTimeOffset.UtcNow);
        TestAggregateState state = new()
        {
            InFlightCommands = ImmutableHashSet.Create("cmd-123"),
            CommandHistory = ImmutableList.Create(entry),
        };
        TestCommandSucceededAction action = new("cmd-123", DateTimeOffset.UtcNow.AddSeconds(5));

        // Act
        TestAggregateState result = AggregateCommandStateReducers.ReduceCommandSucceeded(state, action);

        // Assert
        Assert.True(result.LastCommandSucceeded);
    }

    /// <summary>
    ///     ReduceCommandSucceeded throws on null state.
    /// </summary>
    [Fact]
        public void ReduceCommandSucceededThrowsOnNullState()
    {
        // Arrange
        TestCommandSucceededAction action = new("cmd-123", DateTimeOffset.UtcNow);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            AggregateCommandStateReducers.ReduceCommandSucceeded<TestAggregateState>(null!, action));
    }
}