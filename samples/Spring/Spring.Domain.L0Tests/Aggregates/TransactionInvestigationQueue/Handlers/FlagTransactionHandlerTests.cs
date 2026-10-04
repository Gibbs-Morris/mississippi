using System.Collections.Generic;

using Microsoft.Extensions.Time.Testing;

using Mississippi.DomainModeling.Abstractions;

using MississippiSamples.Spring.Domain.Aggregates.TransactionInvestigationQueue;
using MississippiSamples.Spring.Domain.Aggregates.TransactionInvestigationQueue.Commands;
using MississippiSamples.Spring.Domain.Aggregates.TransactionInvestigationQueue.Events;
using MississippiSamples.Spring.Domain.Aggregates.TransactionInvestigationQueue.Handlers;


namespace MississippiSamples.Spring.Domain.L0Tests.Aggregates.TransactionInvestigationQueue.Handlers;

/// <summary>
///     Tests for <see cref="FlagTransactionHandler" />.
/// </summary>
public sealed class FlagTransactionHandlerTests
{
    private static readonly DateTimeOffset FlaggedTimestamp = new(2025, 1, 15, 11, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset TestTimestamp = new(2025, 1, 15, 10, 30, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider fakeTimeProvider = new(FlaggedTimestamp);

    private readonly FlagTransactionHandler handler;

    /// <summary>
    ///     Initializes a new instance of the <see cref="FlagTransactionHandlerTests" /> class.
    /// </summary>
    public FlagTransactionHandlerTests() => handler = new(fakeTimeProvider);

    /// <summary>
    ///     Flagging preserves the original transaction timestamp.
    /// </summary>
    [Fact]
    public void FlagPreservesOriginalTransactionTimestamp()
    {
        // Arrange
        DateTimeOffset originalTime = new(2025, 6, 15, 14, 30, 0, TimeSpan.Zero);
        FlagTransaction command = new()
        {
            AccountId = "acc-123",
            Amount = 20_000m,
            Timestamp = originalTime,
        };

        // Act
        IReadOnlyList<object> events = handler.ShouldSucceed(null, command);

        // Assert
        TransactionFlagged flagged = Assert.IsType<TransactionFlagged>(events[0]);
        Assert.Equal(originalTime, flagged.OriginalTimestamp);
        Assert.NotEqual(originalTime, flagged.FlaggedTimestamp);
    }

    /// <summary>
    ///     Flagging a transaction with existing aggregate state should still succeed.
    /// </summary>
    [Fact]
    public void FlagTransactionWithExistingStateSucceeds()
    {
        // Arrange
        TransactionInvestigationQueueAggregate existingState = new()
        {
            TotalFlaggedCount = 5,
        };
        FlagTransaction command = new()
        {
            AccountId = "acc-456",
            Amount = 25_000m,
            Timestamp = TestTimestamp,
        };

        // Act
        IReadOnlyList<object> events = handler.ShouldSucceed(existingState, command);

        // Assert
        object item = Assert.Single(events);
        TransactionFlagged flagged = Assert.IsType<TransactionFlagged>(item);
        Assert.Equal("acc-456", flagged.AccountId);
        Assert.Equal(25_000m, flagged.Amount);
    }

    /// <summary>
    ///     Flagging a valid transaction should emit TransactionFlagged event.
    /// </summary>
    [Fact]
    public void FlagValidTransactionEmitsTransactionFlagged()
    {
        // Arrange
        FlagTransaction command = new()
        {
            AccountId = "acc-123",
            Amount = 15_000m,
            Timestamp = TestTimestamp,
        };

        // Act
        IReadOnlyList<object> events = handler.ShouldSucceed(null, command);

        // Assert
        object item = Assert.Single(events);
        TransactionFlagged flagged = Assert.IsType<TransactionFlagged>(item);
        Assert.Equal("acc-123", flagged.AccountId);
        Assert.Equal(15_000m, flagged.Amount);
        Assert.Equal(TestTimestamp, flagged.OriginalTimestamp);
        Assert.Equal(FlaggedTimestamp, flagged.FlaggedTimestamp);
    }

    /// <summary>
    ///     Flagging with empty account ID should fail with InvalidCommand.
    /// </summary>
    [Fact]
    public void FlagWithEmptyAccountIdFailsWithInvalidCommand()
    {
        // Arrange
        FlagTransaction command = new()
        {
            AccountId = string.Empty,
            Amount = 15_000m,
            Timestamp = TestTimestamp,
        };

        // Act & Assert
        handler.ShouldFailWithMessage(null, command, AggregateErrorCodes.InvalidCommand, "Account ID is required");
    }

    /// <summary>
    ///     Flagging with negative amount should fail with InvalidCommand.
    /// </summary>
    [Fact]
    public void FlagWithNegativeAmountFailsWithInvalidCommand()
    {
        // Arrange
        FlagTransaction command = new()
        {
            AccountId = "acc-123",
            Amount = -100m,
            Timestamp = TestTimestamp,
        };

        // Act & Assert
        handler.ShouldFailWithMessage(null, command, AggregateErrorCodes.InvalidCommand, "Amount must be positive");
    }

    /// <summary>
    ///     Flagging with very large amount should succeed.
    /// </summary>
    [Fact]
    public void FlagWithVeryLargeAmountSucceeds()
    {
        // Arrange
        FlagTransaction command = new()
        {
            AccountId = "acc-123",
            Amount = 10_000_000m,
            Timestamp = TestTimestamp,
        };

        // Act
        IReadOnlyList<object> events = handler.ShouldSucceed(null, command);

        // Assert
        object item = Assert.Single(events);
        TransactionFlagged flagged = Assert.IsType<TransactionFlagged>(item);
        Assert.Equal(10_000_000m, flagged.Amount);
    }

    /// <summary>
    ///     Flagging with whitespace-only account ID should fail with InvalidCommand.
    /// </summary>
    [Fact]
    public void FlagWithWhitespaceAccountIdFailsWithInvalidCommand()
    {
        // Arrange
        FlagTransaction command = new()
        {
            AccountId = "   ",
            Amount = 15_000m,
            Timestamp = TestTimestamp,
        };

        // Act & Assert
        handler.ShouldFailWithMessage(null, command, AggregateErrorCodes.InvalidCommand, "Account ID is required");
    }

    /// <summary>
    ///     Flagging with zero amount should fail with InvalidCommand.
    /// </summary>
    [Fact]
    public void FlagWithZeroAmountFailsWithInvalidCommand()
    {
        // Arrange
        FlagTransaction command = new()
        {
            AccountId = "acc-123",
            Amount = 0m,
            Timestamp = TestTimestamp,
        };

        // Act & Assert
        handler.ShouldFailWithMessage(null, command, AggregateErrorCodes.InvalidCommand, "Amount must be positive");
    }
}