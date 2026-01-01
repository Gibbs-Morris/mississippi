// <copyright file="EditMessageHandlerTests.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

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
///     Tests for <see cref="EditMessageHandler" />.
/// </summary>
[AllureSuite("Conversation")]
[AllureSubSuite("Handlers")]
[AllureFeature("EditMessage")]
public sealed class EditMessageHandlerTests
{
    /// <summary>
    ///     Verifies that editing by a non-sender returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle EditMessage when editor is not sender")]
    public void HandleReturnsErrorWhenEditorIsNotSender()
    {
        // Arrange
        EditMessageHandler handler = new();
        EditMessage command = new()
        {
            MessageId = "msg-001",
            NewContent = "Updated content",
            EditedBy = "other-user",
        };
        Message existingMessage = new()
        {
            MessageId = "msg-001",
            Content = "Original content",
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
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, existingState);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that editing a deleted message returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle EditMessage when message deleted")]
    public void HandleReturnsErrorWhenMessageDeleted()
    {
        // Arrange
        EditMessageHandler handler = new();
        EditMessage command = new()
        {
            MessageId = "msg-001",
            NewContent = "Updated content",
            EditedBy = "user-789",
        };
        Message deletedMessage = new()
        {
            MessageId = "msg-001",
            Content = "Original content",
            SentBy = "user-789",
            SentAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            IsDeleted = true,
        };
        ConversationState existingState = new()
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
    ///     Verifies that editing with an empty message ID returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle EditMessage with empty message ID")]
    public void HandleReturnsErrorWhenMessageIdEmpty()
    {
        // Arrange
        EditMessageHandler handler = new();
        EditMessage command = new()
        {
            MessageId = string.Empty,
            NewContent = "Updated content",
            EditedBy = "user-789",
        };
        ConversationState existingState = new()
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
    ///     Verifies that editing a non-existent message returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle EditMessage when message not found")]
    public void HandleReturnsErrorWhenMessageNotFound()
    {
        // Arrange
        EditMessageHandler handler = new();
        EditMessage command = new()
        {
            MessageId = "msg-nonexistent",
            NewContent = "Updated content",
            EditedBy = "user-789",
        };
        ConversationState existingState = new()
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
    ///     Verifies that editing with empty new content returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle EditMessage with empty new content")]
    public void HandleReturnsErrorWhenNewContentEmpty()
    {
        // Arrange
        EditMessageHandler handler = new();
        EditMessage command = new()
        {
            MessageId = "msg-001",
            NewContent = string.Empty,
            EditedBy = "user-789",
        };
        ConversationState existingState = new()
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
    ///     Verifies that editing a message returns a MessageEdited event.
    /// </summary>
    [Fact]
    [AllureStep("Handle EditMessage when message exists")]
    public void HandleReturnsMessageEditedEventWhenMessageExists()
    {
        // Arrange
        EditMessageHandler handler = new();
        EditMessage command = new()
        {
            MessageId = "msg-001",
            NewContent = "Updated content",
            EditedBy = "user-789",
        };
        Message existingMessage = new()
        {
            MessageId = "msg-001",
            Content = "Original content",
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
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, existingState);

        // Assert
        Assert.True(result.Success);
        object singleEvent = Assert.Single(result.Value!);
        MessageEdited edited = Assert.IsType<MessageEdited>(singleEvent);
        Assert.Equal("msg-001", edited.MessageId);
        Assert.Equal("Updated content", edited.NewContent);
        Assert.Equal("user-789", edited.EditedBy);
    }
}