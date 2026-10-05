using System;

using Microsoft.Extensions.Time.Testing;

using Mississippi.Inlet.Client.Abstractions.Commands;


namespace Mississippi.Inlet.Client.L0Tests;

/// <summary>
///     Tests for CommandHistoryEntry.
/// </summary>
public sealed class CommandHistoryEntryTests
{
    private static readonly DateTimeOffset BaseTime = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    ///     CreateExecuting creates entry with Executing status.
    /// </summary>
    [Fact]
    public void CreateExecutingCreatesEntryWithExecutingStatus()
    {
        // Arrange
        string commandId = "cmd-123";
        string commandType = "TestCommand";
        FakeTimeProvider timeProvider = new(BaseTime);
        DateTimeOffset startedAt = timeProvider.GetUtcNow();

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
        FakeTimeProvider timeProvider = new(BaseTime);

        // Act
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            commandId,
            "TestCommand",
            timeProvider.GetUtcNow());

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
        FakeTimeProvider timeProvider = new(BaseTime);

        // Act
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            commandType,
            timeProvider.GetUtcNow());

        // Assert
        Assert.Equal(commandType, entry.CommandType);
    }

    /// <summary>
    ///     CreateExecuting sets CompletedAt to null.
    /// </summary>
    [Fact]
    public void CreateExecutingSetsCompletedAtToNull()
    {
        FakeTimeProvider timeProvider = new(BaseTime);

        // Act
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            timeProvider.GetUtcNow());

        // Assert
        Assert.Null(entry.CompletedAt);
    }

    /// <summary>
    ///     CreateExecuting sets ErrorCode to null.
    /// </summary>
    [Fact]
    public void CreateExecutingSetsErrorCodeToNull()
    {
        FakeTimeProvider timeProvider = new(BaseTime);

        // Act
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            timeProvider.GetUtcNow());

        // Assert
        Assert.Null(entry.ErrorCode);
    }

    /// <summary>
    ///     CreateExecuting sets ErrorMessage to null.
    /// </summary>
    [Fact]
    public void CreateExecutingSetsErrorMessageToNull()
    {
        FakeTimeProvider timeProvider = new(BaseTime);

        // Act
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            timeProvider.GetUtcNow());

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
        FakeTimeProvider timeProvider = new(BaseTime);
        DateTimeOffset startedAt = timeProvider.GetUtcNow();

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
        FakeTimeProvider timeProvider = new(BaseTime);
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            timeProvider.GetUtcNow());
        timeProvider.Advance(TimeSpan.FromSeconds(5));

        // Act
        CommandHistoryEntry failed = entry.ToFailed(timeProvider.GetUtcNow(), null, "Error");

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
        FakeTimeProvider timeProvider = new(BaseTime);
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            timeProvider.GetUtcNow());
        timeProvider.Advance(TimeSpan.FromSeconds(5));

        // Act
        CommandHistoryEntry failed = entry.ToFailed(timeProvider.GetUtcNow(), "ERR", null);

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
        FakeTimeProvider timeProvider = new(BaseTime);
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            commandId,
            "TestCommand",
            timeProvider.GetUtcNow());
        timeProvider.Advance(TimeSpan.FromSeconds(5));

        // Act
        CommandHistoryEntry failed = entry.ToFailed(timeProvider.GetUtcNow(), "ERR", "Error");

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
        FakeTimeProvider timeProvider = new(BaseTime);
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            timeProvider.GetUtcNow());
        timeProvider.Advance(TimeSpan.FromSeconds(5));
        DateTimeOffset completedAt = timeProvider.GetUtcNow();

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
        FakeTimeProvider timeProvider = new(BaseTime);
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            timeProvider.GetUtcNow());
        timeProvider.Advance(TimeSpan.FromMinutes(5));
        DateTimeOffset completedAt = timeProvider.GetUtcNow();

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
        FakeTimeProvider timeProvider = new(BaseTime);
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            timeProvider.GetUtcNow());
        string errorCode = "INSUFFICIENT_FUNDS";
        timeProvider.Advance(TimeSpan.FromSeconds(5));

        // Act
        CommandHistoryEntry failed = entry.ToFailed(timeProvider.GetUtcNow(), errorCode, "Error");

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
        FakeTimeProvider timeProvider = new(BaseTime);
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            timeProvider.GetUtcNow());
        string errorMessage = "Insufficient funds for withdrawal";
        timeProvider.Advance(TimeSpan.FromSeconds(5));

        // Act
        CommandHistoryEntry failed = entry.ToFailed(timeProvider.GetUtcNow(), "ERR001", errorMessage);

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
        FakeTimeProvider timeProvider = new(BaseTime);
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            commandId,
            "TestCommand",
            timeProvider.GetUtcNow());
        timeProvider.Advance(TimeSpan.FromSeconds(5));

        // Act
        CommandHistoryEntry succeeded = entry.ToSucceeded(timeProvider.GetUtcNow());

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
        FakeTimeProvider timeProvider = new(BaseTime);
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            commandType,
            timeProvider.GetUtcNow());
        timeProvider.Advance(TimeSpan.FromSeconds(5));

        // Act
        CommandHistoryEntry succeeded = entry.ToSucceeded(timeProvider.GetUtcNow());

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
        FakeTimeProvider timeProvider = new(BaseTime);
        DateTimeOffset startedAt = timeProvider.GetUtcNow();
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting("cmd-123", "TestCommand", startedAt);
        timeProvider.Advance(TimeSpan.FromSeconds(5));

        // Act
        CommandHistoryEntry succeeded = entry.ToSucceeded(timeProvider.GetUtcNow());

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
        FakeTimeProvider timeProvider = new(BaseTime);
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            timeProvider.GetUtcNow());
        timeProvider.Advance(TimeSpan.FromSeconds(5));

        // Act
        CommandHistoryEntry succeeded = entry.ToSucceeded(timeProvider.GetUtcNow());

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
        FakeTimeProvider timeProvider = new(BaseTime);
        CommandHistoryEntry entry = CommandHistoryEntry.CreateExecuting(
            "cmd-123",
            "TestCommand",
            timeProvider.GetUtcNow());
        timeProvider.Advance(TimeSpan.FromMinutes(5));
        DateTimeOffset completedAt = timeProvider.GetUtcNow();

        // Act
        CommandHistoryEntry succeeded = entry.ToSucceeded(completedAt);

        // Assert
        Assert.Equal(completedAt, succeeded.CompletedAt);
    }
}