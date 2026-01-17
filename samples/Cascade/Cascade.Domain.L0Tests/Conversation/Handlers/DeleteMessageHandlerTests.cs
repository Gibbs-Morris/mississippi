using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.Conversation;
using Cascade.Domain.Conversation.Commands;
using Cascade.Domain.Conversation.Events;
using Cascade.Domain.Conversation.Handlers;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.L0Tests.Conversation.Handlers;

/// <summary>
///     Tests for <see cref="DeleteMessageHandler" />.
/// </summary>
[AllureParentSuite("Cascade")]
[AllureSuite("Conversation")]
[AllureSubSuite("Handlers")]
[AllureFeature("DeleteMessage")]
public sealed class DeleteMessageHandlerTests
{
    /// <summary>
    ///     Verifies that deleting an already deleted message returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle DeleteMessage when message already deleted")]
    public void HandleReturnsErrorWhenMessageAlreadyDeleted()
    {
        // Arrange
        DeleteMessageHandler handler = new();
        DeleteMessage command = new()
        {
            MessageId = "msg-001",
            DeletedBy = "user-789",
        };
        Message deletedMessage = new()
        {
            MessageId = "msg-001",
            Content = "Message content",
            SentBy = "user-789",
            SentAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            IsDeleted = true,
        };
        ConversationAggregate existingState = new()
        {
            IsStarted = true,
            ConversationId = "conv-123",
            ChannelId = "channel-456",
            Messages = ImmutableList.Create(deletedMessage),
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, existingState);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that deleting with an empty message ID returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle DeleteMessage with empty message ID")]
    public void HandleReturnsErrorWhenMessageIdEmpty()
    {
        // Arrange
        DeleteMessageHandler handler = new();
        DeleteMessage command = new()
        {
            MessageId = string.Empty,
            DeletedBy = "user-789",
        };
        ConversationAggregate existingState = new()
        {
            IsStarted = true,
            ConversationId = "conv-123",
            ChannelId = "channel-456",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, existingState);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that deleting a non-existent message returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle DeleteMessage when message not found")]
    public void HandleReturnsErrorWhenMessageNotFound()
    {
        // Arrange
        DeleteMessageHandler handler = new();
        DeleteMessage command = new()
        {
            MessageId = "msg-nonexistent",
            DeletedBy = "user-789",
        };
        ConversationAggregate existingState = new()
        {
            IsStarted = true,
            ConversationId = "conv-123",
            ChannelId = "channel-456",
            Messages = ImmutableList<Message>.Empty,
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, existingState);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that deleting a message returns a MessageDeleted event.
    /// </summary>
    [Fact]
    [AllureStep("Handle DeleteMessage when message exists")]
    public void HandleReturnsMessageDeletedEventWhenMessageExists()
    {
        // Arrange
        DeleteMessageHandler handler = new();
        DeleteMessage command = new()
        {
            MessageId = "msg-001",
            DeletedBy = "user-789",
        };
        Message existingMessage = new()
        {
            MessageId = "msg-001",
            Content = "Message content",
            SentBy = "user-789",
            SentAt = DateTimeOffset.UtcNow.AddMinutes(-5),
        };
        ConversationAggregate existingState = new()
        {
            IsStarted = true,
            ConversationId = "conv-123",
            ChannelId = "channel-456",
            Messages = ImmutableList.Create(existingMessage),
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, existingState);

        // Assert
        Assert.True(result.Success);
        object singleEvent = Assert.Single(result.Value!);
        MessageDeleted deleted = Assert.IsType<MessageDeleted>(singleEvent);
        Assert.Equal("msg-001", deleted.MessageId);
        Assert.Equal("user-789", deleted.DeletedBy);
    }
}