using System;
using System.Collections.Immutable;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.Conversation;
using Cascade.Domain.Conversation.Events;
using Cascade.Domain.Conversation.Reducers;


namespace Cascade.Domain.L0Tests.Conversation.Reducers;

/// <summary>
///     Tests for <see cref="MessageEditedReducer" />.
/// </summary>
[AllureParentSuite("Cascade")]
[AllureSuite("Conversation")]
[AllureSubSuite("Reducers")]
[AllureFeature("MessageEdited")]
public sealed class MessageEditedReducerTests
{
    /// <summary>
    ///     Verifies that reducing a MessageEdited event only updates the target message.
    /// </summary>
    [Fact]
    [AllureStep("Reduce MessageEdited only updates target message")]
    public void ReduceOnlyUpdatesTargetMessage()
    {
        // Arrange
        MessageEditedReducer reducer = new();
        DateTimeOffset editedAt = DateTimeOffset.UtcNow;
        MessageEdited evt = new()
        {
            MessageId = "msg-002",
            NewContent = "Updated second message",
            EditedBy = "user-222",
            EditedAt = editedAt,
        };
        Message message1 = new()
        {
            MessageId = "msg-001",
            Content = "First message",
            SentBy = "user-111",
            SentAt = DateTimeOffset.UtcNow.AddMinutes(-10),
        };
        Message message2 = new()
        {
            MessageId = "msg-002",
            Content = "Second message",
            SentBy = "user-222",
            SentAt = DateTimeOffset.UtcNow.AddMinutes(-5),
        };
        ConversationAggregate existingState = new()
        {
            IsStarted = true,
            ConversationId = "conv-123",
            ChannelId = "channel-456",
            Messages = ImmutableList.Create(message1, message2),
        };

        // Act
        ConversationAggregate result = reducer.Reduce(existingState, evt);

        // Assert
        Assert.Equal("First message", result.Messages[0].Content);
        Assert.Equal("Updated second message", result.Messages[1].Content);
    }

    /// <summary>
    ///     Verifies that reducing a MessageEdited event preserves other message properties.
    /// </summary>
    [Fact]
    [AllureStep("Reduce MessageEdited preserves other properties")]
    public void ReducePreservesOtherProperties()
    {
        // Arrange
        MessageEditedReducer reducer = new();
        DateTimeOffset sentAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        DateTimeOffset editedAt = DateTimeOffset.UtcNow;
        MessageEdited evt = new()
        {
            MessageId = "msg-001",
            NewContent = "Updated content",
            EditedBy = "user-789",
            EditedAt = editedAt,
        };
        Message existingMessage = new()
        {
            MessageId = "msg-001",
            Content = "Original content",
            SentBy = "user-789",
            SentAt = sentAt,
        };
        ConversationAggregate existingState = new()
        {
            IsStarted = true,
            ConversationId = "conv-123",
            ChannelId = "channel-456",
            Messages = ImmutableList.Create(existingMessage),
        };

        // Act
        ConversationAggregate result = reducer.Reduce(existingState, evt);

        // Assert
        Assert.Equal("msg-001", result.Messages[0].MessageId);
        Assert.Equal("user-789", result.Messages[0].SentBy);
        Assert.Equal(sentAt, result.Messages[0].SentAt);
        Assert.False(result.Messages[0].IsDeleted);
    }

    /// <summary>
    ///     Verifies that reducing a MessageEdited event returns unchanged state when message not found.
    /// </summary>
    [Fact]
    [AllureStep("Reduce MessageEdited returns unchanged state when message not found")]
    public void ReduceReturnsUnchangedStateWhenMessageNotFound()
    {
        // Arrange
        MessageEditedReducer reducer = new();
        MessageEdited evt = new()
        {
            MessageId = "msg-nonexistent",
            NewContent = "Updated content",
            EditedBy = "user-789",
            EditedAt = DateTimeOffset.UtcNow,
        };
        ConversationAggregate existingState = new()
        {
            IsStarted = true,
            ConversationId = "conv-123",
            ChannelId = "channel-456",
            Messages = ImmutableList<Message>.Empty,
        };

        // Act
        ConversationAggregate result = reducer.Reduce(existingState, evt);

        // Assert - Reducers must return new instance, but values should be equivalent
        Assert.NotSame(existingState, result);
        Assert.Equal(existingState.ConversationId, result.ConversationId);
        Assert.Equal(existingState.ChannelId, result.ChannelId);
        Assert.Equal(existingState.IsStarted, result.IsStarted);
        Assert.Equal(existingState.Messages, result.Messages);
    }

    /// <summary>
    ///     Verifies that reducing a MessageEdited event updates the message content.
    /// </summary>
    [Fact]
    [AllureStep("Reduce MessageEdited updates message content")]
    public void ReduceUpdatesMessageContent()
    {
        // Arrange
        MessageEditedReducer reducer = new();
        DateTimeOffset editedAt = DateTimeOffset.UtcNow;
        MessageEdited evt = new()
        {
            MessageId = "msg-001",
            NewContent = "Updated content",
            EditedBy = "user-789",
            EditedAt = editedAt,
        };
        Message existingMessage = new()
        {
            MessageId = "msg-001",
            Content = "Original content",
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
        ConversationAggregate result = reducer.Reduce(existingState, evt);

        // Assert
        Assert.Equal("Updated content", result.Messages[0].Content);
        Assert.Equal(editedAt, result.Messages[0].EditedAt);
    }
}