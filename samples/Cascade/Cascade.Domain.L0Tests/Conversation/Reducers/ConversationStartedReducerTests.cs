using System;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.Conversation;
using Cascade.Domain.Conversation.Events;
using Cascade.Domain.Conversation.Reducers;


namespace Cascade.Domain.L0Tests.Conversation.Reducers;

/// <summary>
///     Tests for <see cref="ConversationStartedReducer" />.
/// </summary>
[AllureSuite("Conversation")]
[AllureSubSuite("Reducers")]
[AllureFeature("ConversationStarted")]
public sealed class ConversationStartedReducerTests
{
    /// <summary>
    ///     Verifies that reducing a ConversationStarted event creates a started state.
    /// </summary>
    [Fact]
    [AllureStep("Reduce ConversationStarted creates started state")]
    public void ReduceCreatesStartedState()
    {
        // Arrange
        ConversationStartedReducer reducer = new();
        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        ConversationStarted evt = new()
        {
            ConversationId = "conv-123",
            ChannelId = "channel-456",
            StartedAt = startedAt,
        };

        // Act
        ConversationAggregate result = reducer.Reduce(null!, evt);

        // Assert
        Assert.True(result.IsStarted);
        Assert.Equal("conv-123", result.ConversationId);
        Assert.Equal("channel-456", result.ChannelId);
        Assert.Equal(startedAt, result.StartedAt);
        Assert.Empty(result.Messages);
    }

    /// <summary>
    ///     Verifies that reducing a ConversationStarted event overwrites existing state.
    /// </summary>
    [Fact]
    [AllureStep("Reduce ConversationStarted overwrites existing state")]
    public void ReduceOverwritesExistingState()
    {
        // Arrange
        ConversationStartedReducer reducer = new();
        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        ConversationStarted evt = new()
        {
            ConversationId = "conv-new",
            ChannelId = "channel-new",
            StartedAt = startedAt,
        };
        ConversationAggregate existingState = new()
        {
            IsStarted = true,
            ConversationId = "conv-old",
            ChannelId = "channel-old",
        };

        // Act
        ConversationAggregate result = reducer.Reduce(existingState, evt);

        // Assert
        Assert.Equal("conv-new", result.ConversationId);
        Assert.Equal("channel-new", result.ChannelId);
    }
}