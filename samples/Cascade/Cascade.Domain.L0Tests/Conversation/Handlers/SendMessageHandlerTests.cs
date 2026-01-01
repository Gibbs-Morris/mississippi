// <copyright file="SendMessageHandlerTests.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System.Collections.Generic;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.Conversation;
using Cascade.Domain.Conversation.Commands;
using Cascade.Domain.Conversation.Events;
using Cascade.Domain.Conversation.Handlers;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.L0Tests.Conversation.Handlers;

/// <summary>
///     Tests for <see cref="SendMessageHandler" />.
/// </summary>
[AllureSuite("Conversation")]
[AllureSubSuite("Handlers")]
[AllureFeature("SendMessage")]
public sealed class SendMessageHandlerTests
{
    /// <summary>
    ///     Verifies that sending with empty content returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle SendMessage with empty content")]
    public void HandleReturnsErrorWhenContentEmpty()
    {
        // Arrange
        SendMessageHandler handler = new();
        SendMessage command = new()
        {
            MessageId = "msg-001",
            Content = string.Empty,
            SentBy = "user-789",
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
    ///     Verifies that sending a message without a started conversation returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle SendMessage when conversation not started")]
    public void HandleReturnsErrorWhenConversationNotStarted()
    {
        // Arrange
        SendMessageHandler handler = new();
        SendMessage command = new()
        {
            MessageId = "msg-001",
            Content = "Hello!",
            SentBy = "user-789",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that sending with an empty message ID returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle SendMessage with empty message ID")]
    public void HandleReturnsErrorWhenMessageIdEmpty()
    {
        // Arrange
        SendMessageHandler handler = new();
        SendMessage command = new()
        {
            MessageId = string.Empty,
            Content = "Hello!",
            SentBy = "user-789",
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
    ///     Verifies that sending with an empty sender ID returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle SendMessage with empty sender")]
    public void HandleReturnsErrorWhenSentByEmpty()
    {
        // Arrange
        SendMessageHandler handler = new();
        SendMessage command = new()
        {
            MessageId = "msg-001",
            Content = "Hello!",
            SentBy = string.Empty,
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
    ///     Verifies that sending a message in a started conversation returns a MessageSent event.
    /// </summary>
    [Fact]
    [AllureStep("Handle SendMessage when conversation started")]
    public void HandleReturnsMessageSentEventWhenConversationStarted()
    {
        // Arrange
        SendMessageHandler handler = new();
        SendMessage command = new()
        {
            MessageId = "msg-001",
            Content = "Hello, world!",
            SentBy = "user-789",
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
        Assert.True(result.Success);
        object singleEvent = Assert.Single(result.Value!);
        MessageSent sent = Assert.IsType<MessageSent>(singleEvent);
        Assert.Equal("msg-001", sent.MessageId);
        Assert.Equal("Hello, world!", sent.Content);
        Assert.Equal("user-789", sent.SentBy);
    }
}