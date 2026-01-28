using System;


using Mississippi.Inlet.Client.Abstractions.Commands;


namespace Mississippi.Inlet.Client.L0Tests;

/// <summary>
///     Tests for CommandHistoryEntry.
/// </summary>
public sealed class CommandHistoryEntryTests
{
    /// <summary>
    ///     CreateExecuting creates entry with Executing status.
    /// </summary>
    [Fact]
        public void CreateExecutingCreatesEntryWithExecutingStatus()
    {
        // Arrange
        string commandId = "cmd-123";
        string commandType = "TestCommand";
        DateTimeOffset startedAt = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

        // Act
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(commandId, commandType, startedAt);

        // Assert
        Assert.Equal(CommandStatus.Executing, entry.Status);
    }

    /// <summary>
    ///     CreateExecuting sets CommandId.
    /// </summary>
    [Fact]
        public void CreateExecutingSetsCommandId()
    {
        // Arrange
        string commandId = "cmd-456";

        // Act
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            commandId,
            "TestCommand",
            DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(commandId, entry.CommandId);
    }

    /// <summary>
    ///     CreateExecuting sets CommandType.
    /// </summary>
    [Fact]
        public void CreateExecutingSetsCommandType()
    {
        // Arrange
        string commandType = "DepositCommand";

        // Act
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting("cmd-123", commandType, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(commandType, entry.CommandType);
    }

    /// <summary>
    ///     CreateExecuting sets CompletedAt to null.
    /// </summary>
    [Fact]
        public void CreateExecutingSetsCompletedAtToNull()
    {
        // Act
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            DateTimeOffset.UtcNow);

        // Assert
        Assert.Null(entry.CompletedAt);
    }

    /// <summary>
    ///     CreateExecuting sets ErrorCode to null.
    /// </summary>
    [Fact]
        public void CreateExecutingSetsErrorCodeToNull()
    {
        // Act
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            DateTimeOffset.UtcNow);

        // Assert
        Assert.Null(entry.ErrorCode);
    }

    /// <summary>
    ///     CreateExecuting sets ErrorMessage to null.
    /// </summary>
    [Fact]
        public void CreateExecutingSetsErrorMessageToNull()
    {
        // Act
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            DateTimeOffset.UtcNow);

        // Assert
        Assert.Null(entry.ErrorMessage);
    }

    /// <summary>
    ///     CreateExecuting sets StartedAt.
    /// </summary>
    [Fact]
        public void CreateExecutingSetsStartedAt()
    {
        // Arrange
        DateTimeOffset startedAt = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

        // Act
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting("cmd-123", "TestCommand", startedAt);

        // Assert
        Assert.Equal(startedAt, entry.StartedAt);
    }

    /// <summary>
    ///     ToFailed allows null ErrorCode.
    /// </summary>
    [Fact]
        public void ToFailedAllowsNullErrorCode()
    {
        // Arrange
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            DateTimeOffset.UtcNow);

        // Act
        CommandHistoryEntry failed = entry.ToFailed(DateTimeOffset.UtcNow, null, "Error");

        // Assert
        Assert.Null(failed.ErrorCode);
    }

    /// <summary>
    ///     ToFailed allows null ErrorMessage.
    /// </summary>
    [Fact]
        public void ToFailedAllowsNullErrorMessage()
    {
        // Arrange
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            DateTimeOffset.UtcNow);

        // Act
        CommandHistoryEntry failed = entry.ToFailed(DateTimeOffset.UtcNow, "ERR", null);

        // Assert
        Assert.Null(failed.ErrorMessage);
    }

    /// <summary>
    ///     ToFailed preserves original CommandId.
    /// </summary>
    [Fact]
        public void ToFailedPreservesCommandId()
    {
        // Arrange
        string commandId = "cmd-preserve";
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            commandId,
            "TestCommand",
            DateTimeOffset.UtcNow);

        // Act
        CommandHistoryEntry failed = entry.ToFailed(DateTimeOffset.UtcNow, "ERR", "Error");

        // Assert
        Assert.Equal(commandId, failed.CommandId);
    }

    /// <summary>
    ///     ToFailed returns entry with Failed status.
    /// </summary>
    [Fact]
        public void ToFailedReturnsEntryWithFailedStatus()
    {
        // Arrange
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            DateTimeOffset.UtcNow);
        DateTimeOffset completedAt = DateTimeOffset.UtcNow.AddSeconds(5);

        // Act
        CommandHistoryEntry failed = entry.ToFailed(completedAt, "ERR001", "Error message");

        // Assert
        Assert.Equal(CommandStatus.Failed, failed.Status);
    }

    /// <summary>
    ///     ToFailed sets CompletedAt.
    /// </summary>
    [Fact]
        public void ToFailedSetsCompletedAt()
    {
        // Arrange
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            DateTimeOffset.UtcNow);
        DateTimeOffset completedAt = new(2024, 1, 1, 12, 5, 0, TimeSpan.Zero);

        // Act
        CommandHistoryEntry failed = entry.ToFailed(completedAt, "ERR001", "Error");

        // Assert
        Assert.Equal(completedAt, failed.CompletedAt);
    }

    /// <summary>
    ///     ToFailed sets ErrorCode.
    /// </summary>
    [Fact]
        public void ToFailedSetsErrorCode()
    {
        // Arrange
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            DateTimeOffset.UtcNow);
        string errorCode = "INSUFFICIENT_FUNDS";

        // Act
        CommandHistoryEntry failed = entry.ToFailed(DateTimeOffset.UtcNow, errorCode, "Error");

        // Assert
        Assert.Equal(errorCode, failed.ErrorCode);
    }

    /// <summary>
    ///     ToFailed sets ErrorMessage.
    /// </summary>
    [Fact]
        public void ToFailedSetsErrorMessage()
    {
        // Arrange
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            DateTimeOffset.UtcNow);
        string errorMessage = "Insufficient funds for withdrawal";

        // Act
        CommandHistoryEntry failed = entry.ToFailed(DateTimeOffset.UtcNow, "ERR001", errorMessage);

        // Assert
        Assert.Equal(errorMessage, failed.ErrorMessage);
    }

    /// <summary>
    ///     ToSucceeded preserves original CommandId.
    /// </summary>
    [Fact]
        public void ToSucceededPreservesCommandId()
    {
        // Arrange
        string commandId = "cmd-preserve";
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            commandId,
            "TestCommand",
            DateTimeOffset.UtcNow);

        // Act
        CommandHistoryEntry succeeded = entry.ToSucceeded(DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(commandId, succeeded.CommandId);
    }

    /// <summary>
    ///     ToSucceeded preserves CommandType.
    /// </summary>
    [Fact]
        public void ToSucceededPreservesCommandType()
    {
        // Arrange
        string commandType = "DepositCommand";
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting("cmd-123", commandType, DateTimeOffset.UtcNow);

        // Act
        CommandHistoryEntry succeeded = entry.ToSucceeded(DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(commandType, succeeded.CommandType);
    }

    /// <summary>
    ///     ToSucceeded preserves StartedAt.
    /// </summary>
    [Fact]
        public void ToSucceededPreservesStartedAt()
    {
        // Arrange
        DateTimeOffset startedAt = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting("cmd-123", "TestCommand", startedAt);

        // Act
        CommandHistoryEntry succeeded = entry.ToSucceeded(DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(startedAt, succeeded.StartedAt);
    }

    /// <summary>
    ///     ToSucceeded returns entry with Succeeded status.
    /// </summary>
    [Fact]
        public void ToSucceededReturnsEntryWithSucceededStatus()
    {
        // Arrange
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            DateTimeOffset.UtcNow);

        // Act
        CommandHistoryEntry succeeded = entry.ToSucceeded(DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(CommandStatus.Succeeded, succeeded.Status);
    }

    /// <summary>
    ///     ToSucceeded sets CompletedAt.
    /// </summary>
    [Fact]
        public void ToSucceededSetsCompletedAt()
    {
        // Arrange
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            DateTimeOffset.UtcNow);
        DateTimeOffset completedAt = new(2024, 1, 1, 12, 5, 0, TimeSpan.Zero);

        // Act
        CommandHistoryEntry succeeded = entry.ToSucceeded(completedAt);

        // Assert
        Assert.Equal(completedAt, succeeded.CompletedAt);
    }
}