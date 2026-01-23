using System;
using System.Collections.Immutable;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.Aggregates.Conversation;
using Cascade.Domain.Aggregates.Conversation.Events;
using Cascade.Domain.Aggregates.Conversation.Reducers;


namespace Cascade.Domain.L0Tests.Conversation.Reducers;

/// <summary>
///     Tests for <see cref="MessageSentEventReducer" />.
/// </summary>
[AllureParentSuite("Cascade")]
[AllureSuite("Conversation")]
[AllureSubSuite("Reducers")]
[AllureFeature("MessageSent")]
public sealed class MessageSentEventReducerTests
{
    /// <summary>
    ///     Verifies that reducing a MessageSent event adds a message to the state.
    /// </summary>
    [Fact]
    [AllureStep("Reduce MessageSent adds message to state")]
    public void ReduceAddsMessageToState()
    {
        // Arrange
        MessageSentEventReducer eventReducer = new();
        DateTimeOffset sentAt = DateTimeOffset.UtcNow;
        MessageSent evt = new()
        {
            MessageId = "msg-001",
            Content = "Hello, world!",
            SentBy = "user-789",
            SentAt = sentAt,
        };
        ConversationAggregate existingState = new()
        {
            IsStarted = true,
            ConversationId = "conv-123",
            ChannelId = "channel-456",
            Messages = ImmutableList<Message>.Empty,
        };

        // Act
        ConversationAggregate result = eventReducer.Reduce(existingState, evt);

        // Assert
        Assert.Single(result.Messages);
        Assert.Equal("msg-001", result.Messages[0].MessageId);
        Assert.Equal("Hello, world!", result.Messages[0].Content);
        Assert.Equal("user-789", result.Messages[0].SentBy);
        Assert.Equal(sentAt, result.Messages[0].SentAt);
        Assert.False(result.Messages[0].IsDeleted);
        Assert.Null(result.Messages[0].EditedAt);
    }

    /// <summary>
    ///     Verifies that reducing a MessageSent event appends to existing messages.
    /// </summary>
    [Fact]
    [AllureStep("Reduce MessageSent appends to existing messages")]
    public void ReduceAppendsToExistingMessages()
    {
        // Arrange
        MessageSentEventReducer eventReducer = new();
        DateTimeOffset sentAt = DateTimeOffset.UtcNow;
        MessageSent evt = new()
        {
            MessageId = "msg-002",
            Content = "Second message",
            SentBy = "user-222",
            SentAt = sentAt,
        };
        Message existingMessage = new()
        {
            MessageId = "msg-001",
            Content = "First message",
            SentBy = "user-111",
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
        ConversationAggregate result = eventReducer.Reduce(existingState, evt);

        // Assert
        Assert.Equal(2, result.Messages.Count);
        Assert.Equal("msg-001", result.Messages[0].MessageId);
        Assert.Equal("msg-002", result.Messages[1].MessageId);
    }
}