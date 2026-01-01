// <copyright file="MessageDeletedReducerTests.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;
using System.Collections.Immutable;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Crescent.NewModel.Chat;
using Crescent.NewModel.Chat.Events;
using Crescent.NewModel.Chat.Reducers;

using Xunit;


namespace Crescent.NewModel.L0Tests.Chat.Reducers;

/// <summary>
///     Tests for <see cref="MessageDeletedReducer" />.
/// </summary>
[AllureParentSuite("Crescent")]
[AllureSuite("NewModel")]
[AllureSubSuite("MessageDeletedReducer")]
public sealed class MessageDeletedReducerTests
{
    /// <summary>
    ///     Verifies that reducing a MessageDeleted event removes the message from state.
    /// </summary>
    [Fact]
    [AllureStep("Reduce MessageDeleted event removes message from state")]
    public void ReduceMessageDeletedEventRemovesMessageFromState()
    {
        // Arrange
        MessageDeletedReducer reducer = new();
        MessageDeleted @event = new()
        {
            MessageId = "msg-001",
        };
        ChatState existingState = new()
        {
            IsCreated = true,
            Name = "General",
            Messages =
            [
                new ChatMessage
                {
                    MessageId = "msg-001",
                    Content = "Hello!",
                    Author = "user1",
                    CreatedAt = DateTimeOffset.UtcNow,
                },
                new ChatMessage
                {
                    MessageId = "msg-002",
                    Content = "World!",
                    Author = "user2",
                    CreatedAt = DateTimeOffset.UtcNow,
                },
            ],
        };

        // Act
        ChatState result = reducer.Reduce(existingState, @event);

        // Assert
        Assert.Single(result.Messages);
        Assert.Equal("msg-002", result.Messages[0].MessageId);
    }

    /// <summary>
    ///     Verifies that reducing when message is not in state returns unchanged state.
    /// </summary>
    [Fact]
    [AllureStep("Reduce MessageDeleted when message not in state returns unchanged")]
    public void ReduceMessageDeletedWhenMessageNotInStateReturnsUnchanged()
    {
        // Arrange
        MessageDeletedReducer reducer = new();
        MessageDeleted @event = new()
        {
            MessageId = "msg-999",
        };
        ChatState existingState = new()
        {
            IsCreated = true,
            Name = "General",
            Messages =
            [
                new ChatMessage
                {
                    MessageId = "msg-001",
                    Content = "Hello",
                    Author = "user1",
                    CreatedAt = DateTimeOffset.UtcNow,
                },
            ],
        };

        // Act
        ChatState result = reducer.Reduce(existingState, @event);

        // Assert
        Assert.NotSame(existingState, result);
        Assert.Equal(existingState.Messages.Count, result.Messages.Count);
        Assert.Equal(existingState.Messages[0].MessageId, result.Messages[0].MessageId);
    }

    /// <summary>
    ///     Verifies that reducing with a null event throws ArgumentNullException.
    /// </summary>
    [Fact]
    [AllureStep("Reduce with null event throws ArgumentNullException")]
    public void ReduceNullEventThrowsArgumentNullException()
    {
        // Arrange
        MessageDeletedReducer reducer = new();
        ChatState existingState = new()
        {
            IsCreated = true,
            Name = "General",
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => reducer.Reduce(existingState, null!));
    }
}
