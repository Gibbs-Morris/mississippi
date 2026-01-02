using System;
using System.Collections.Generic;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Crescent.NewModel.Chat;
using Crescent.NewModel.Chat.Commands;
using Crescent.NewModel.Chat.Events;
using Crescent.NewModel.Chat.Handlers;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Crescent.NewModel.L0Tests.Chat.Handlers;

/// <summary>
///     Tests for <see cref="DeleteMessageHandler" />.
/// </summary>
[AllureParentSuite("Crescent")]
[AllureSuite("NewModel")]
[AllureSubSuite("DeleteMessageHandler")]
public sealed class DeleteMessageHandlerTests
{
    /// <summary>
    ///     Verifies that deleting a message when chat does not exist fails.
    /// </summary>
    [Fact]
    [AllureStep("DeleteMessage when chat does not exist fails with NotFound")]
    public void DeleteMessageWhenChatDoesNotExistFailsWithNotFound()
    {
        // Arrange
        DeleteMessageHandler handler = new();
        DeleteMessage command = new()
        {
            MessageId = "msg-001",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.NotFound, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that deleting a non-existent message fails.
    /// </summary>
    [Fact]
    [AllureStep("DeleteMessage when message does not exist fails with NotFound")]
    public void DeleteMessageWhenMessageDoesNotExistFailsWithNotFound()
    {
        // Arrange
        DeleteMessageHandler handler = new();
        DeleteMessage command = new()
        {
            MessageId = "msg-999",
        };
        ChatAggregate existingState = new()
        {
            IsCreated = true,
            Name = "General",
            Messages =
            [
                new()
                {
                    MessageId = "msg-001",
                    Content = "Hello!",
                    Author = "user1",
                    CreatedAt = DateTimeOffset.UtcNow,
                },
            ],
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, existingState);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.NotFound, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that deleting with empty message ID fails.
    /// </summary>
    [Fact]
    [AllureStep("DeleteMessage with empty message ID fails with InvalidCommand")]
    public void DeleteMessageWithEmptyMessageIdFailsWithInvalidCommand()
    {
        // Arrange
        DeleteMessageHandler handler = new();
        DeleteMessage command = new()
        {
            MessageId = string.Empty,
        };
        ChatAggregate existingState = new()
        {
            IsCreated = true,
            Name = "General",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, existingState);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that deleting a message produces a MessageDeleted event.
    /// </summary>
    [Fact]
    [AllureStep("DeleteMessage with valid data produces MessageDeleted event")]
    public void DeleteMessageWithValidDataProducesMessageDeletedEvent()
    {
        // Arrange
        DeleteMessageHandler handler = new();
        DeleteMessage command = new()
        {
            MessageId = "msg-001",
        };
        ChatAggregate existingState = new()
        {
            IsCreated = true,
            Name = "General",
            Messages =
            [
                new()
                {
                    MessageId = "msg-001",
                    Content = "Hello!",
                    Author = "user1",
                    CreatedAt = DateTimeOffset.UtcNow,
                },
            ],
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, existingState);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
        MessageDeleted? @event = Assert.IsType<MessageDeleted>(result.Value[0]);
        Assert.Equal("msg-001", @event.MessageId);
    }
}