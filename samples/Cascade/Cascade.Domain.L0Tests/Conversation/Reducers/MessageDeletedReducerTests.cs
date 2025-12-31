// <copyright file="MessageDeletedReducerTests.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;
using System.Collections.Immutable;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.Conversation;
using Cascade.Domain.Conversation.Events;
using Cascade.Domain.Conversation.Reducers;

using Xunit;


namespace Cascade.Domain.L0Tests.Conversation.Reducers;

/// <summary>
///     Tests for <see cref="MessageDeletedReducer" />.
/// </summary>
[AllureSuite("Conversation")]
[AllureSubSuite("Reducers")]
[AllureFeature("MessageDeleted")]
public sealed class MessageDeletedReducerTests
{
    /// <summary>
    ///     Verifies that reducing a MessageDeleted event marks the message as deleted.
    /// </summary>
    [Fact]
    [AllureStep("Reduce MessageDeleted marks message as deleted")]
    public void ReduceMarksMessageAsDeleted()
    {
        // Arrange
        MessageDeletedReducer reducer = new();
        MessageDeleted evt = new()
        {
            MessageId = "msg-001",
            DeletedBy = "user-789",
            DeletedAt = DateTimeOffset.UtcNow,
        };
        Message existingMessage = new()
        {
            MessageId = "msg-001",
            Content = "Message content",
            SentBy = "user-789",
            SentAt = DateTimeOffset.UtcNow.AddMinutes(-5),
        };
        ConversationState existingState = new()
        {
            IsStarted = true,
            ConversationId = "conv-123",
            ChannelId = "channel-456",
            Messages = ImmutableList.Create(existingMessage),
        };

        // Act
        ConversationState result = reducer.Reduce(existingState, evt);

        // Assert
        Assert.True(result.Messages[0].IsDeleted);
    }

    /// <summary>
    ///     Verifies that reducing a MessageDeleted event preserves other message properties.
    /// </summary>
    [Fact]
    [AllureStep("Reduce MessageDeleted preserves other properties")]
    public void ReducePreservesOtherProperties()
    {
        // Arrange
        MessageDeletedReducer reducer = new();
        DateTimeOffset sentAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        DateTimeOffset editedAt = DateTimeOffset.UtcNow.AddMinutes(-2);
        MessageDeleted evt = new()
        {
            MessageId = "msg-001",
            DeletedBy = "user-789",
            DeletedAt = DateTimeOffset.UtcNow,
        };
        Message existingMessage = new()
        {
            MessageId = "msg-001",
            Content = "Message content",
            SentBy = "user-789",
            SentAt = sentAt,
            EditedAt = editedAt,
        };
        ConversationState existingState = new()
        {
            IsStarted = true,
            ConversationId = "conv-123",
            ChannelId = "channel-456",
            Messages = ImmutableList.Create(existingMessage),
        };

        // Act
        ConversationState result = reducer.Reduce(existingState, evt);

        // Assert
        Assert.Equal("msg-001", result.Messages[0].MessageId);
        Assert.Equal("Message content", result.Messages[0].Content);
        Assert.Equal("user-789", result.Messages[0].SentBy);
        Assert.Equal(sentAt, result.Messages[0].SentAt);
        Assert.Equal(editedAt, result.Messages[0].EditedAt);
    }

    /// <summary>
    ///     Verifies that reducing a MessageDeleted event only deletes the target message.
    /// </summary>
    [Fact]
    [AllureStep("Reduce MessageDeleted only deletes target message")]
    public void ReduceOnlyDeletesTargetMessage()
    {
        // Arrange
        MessageDeletedReducer reducer = new();
        MessageDeleted evt = new()
        {
            MessageId = "msg-002",
            DeletedBy = "user-222",
            DeletedAt = DateTimeOffset.UtcNow,
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
        ConversationState existingState = new()
        {
            IsStarted = true,
            ConversationId = "conv-123",
            ChannelId = "channel-456",
            Messages = ImmutableList.Create(message1, message2),
        };

        // Act
        ConversationState result = reducer.Reduce(existingState, evt);

        // Assert
        Assert.False(result.Messages[0].IsDeleted);
        Assert.True(result.Messages[1].IsDeleted);
    }

    /// <summary>
    ///     Verifies that reducing a MessageDeleted event returns unchanged state when message not found.
    /// </summary>
    [Fact]
    [AllureStep("Reduce MessageDeleted returns unchanged state when message not found")]
    public void ReduceReturnsUnchangedStateWhenMessageNotFound()
    {
        // Arrange
        MessageDeletedReducer reducer = new();
        MessageDeleted evt = new()
        {
            MessageId = "msg-nonexistent",
            DeletedBy = "user-789",
            DeletedAt = DateTimeOffset.UtcNow,
        };
        ConversationState existingState = new()
        {
            IsStarted = true,
            ConversationId = "conv-123",
            ChannelId = "channel-456",
            Messages = ImmutableList<Message>.Empty,
        };

        // Act
        ConversationState result = reducer.Reduce(existingState, evt);

        // Assert - Reducers must return new instance, but values should be equivalent
        Assert.NotSame(existingState, result);
        Assert.Equal(existingState.ConversationId, result.ConversationId);
        Assert.Equal(existingState.ChannelId, result.ChannelId);
        Assert.Equal(existingState.IsStarted, result.IsStarted);
        Assert.Equal(existingState.Messages, result.Messages);
    }
}
