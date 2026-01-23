using System.Collections.Generic;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.Aggregates.Conversation;
using Cascade.Domain.Aggregates.Conversation.Commands;
using Cascade.Domain.Aggregates.Conversation.Events;
using Cascade.Domain.Aggregates.Conversation.Handlers;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.L0Tests.Conversation.Handlers;

/// <summary>
///     Tests for <see cref="StartConversationHandler" />.
/// </summary>
[AllureParentSuite("Cascade")]
[AllureSuite("Conversation")]
[AllureSubSuite("Handlers")]
[AllureFeature("StartConversation")]
public sealed class StartConversationHandlerTests
{
    /// <summary>
    ///     Verifies that starting a new conversation returns a ConversationStarted event.
    /// </summary>
    [Fact]
    [AllureStep("Handle StartConversation when not started")]
    public void HandleReturnsConversationStartedEventWhenNotStarted()
    {
        // Arrange
        StartConversationHandler handler = new();
        StartConversation command = new()
        {
            ConversationId = "conv-123",
            ChannelId = "channel-456",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        // Assert
        Assert.True(result.Success);
        object singleEvent = Assert.Single(result.Value!);
        ConversationStarted started = Assert.IsType<ConversationStarted>(singleEvent);
        Assert.Equal("conv-123", started.ConversationId);
        Assert.Equal("channel-456", started.ChannelId);
    }

    /// <summary>
    ///     Verifies that starting a conversation when already started returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle StartConversation when already started")]
    public void HandleReturnsErrorWhenAlreadyStarted()
    {
        // Arrange
        StartConversationHandler handler = new();
        StartConversation command = new()
        {
            ConversationId = "conv-123",
            ChannelId = "channel-456",
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
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that starting with an empty channel ID returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle StartConversation with empty channel ID")]
    public void HandleReturnsErrorWhenChannelIdEmpty()
    {
        // Arrange
        StartConversationHandler handler = new();
        StartConversation command = new()
        {
            ConversationId = "conv-123",
            ChannelId = string.Empty,
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that starting with an empty conversation ID returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle StartConversation with empty conversation ID")]
    public void HandleReturnsErrorWhenConversationIdEmpty()
    {
        // Arrange
        StartConversationHandler handler = new();
        StartConversation command = new()
        {
            ConversationId = string.Empty,
            ChannelId = "channel-456",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
    }
}