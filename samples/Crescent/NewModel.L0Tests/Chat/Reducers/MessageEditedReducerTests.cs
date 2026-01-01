// <copyright file="MessageEditedReducerTests.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Crescent.NewModel.Chat;
using Crescent.NewModel.Chat.Events;
using Crescent.NewModel.Chat.Reducers;


namespace Crescent.NewModel.L0Tests.Chat.Reducers;

/// <summary>
///     Tests for <see cref="MessageEditedReducer" />.
/// </summary>
[AllureParentSuite("Crescent")]
[AllureSuite("NewModel")]
[AllureSubSuite("MessageEditedReducer")]
public sealed class MessageEditedReducerTests
{
    /// <summary>
    ///     Verifies that reducing a MessageEdited event updates the message content.
    /// </summary>
    [Fact]
    [AllureStep("Reduce MessageEdited event updates message content")]
    public void ReduceMessageEditedEventUpdatesMessageContent()
    {
        // Arrange
        MessageEditedReducer reducer = new();
        DateTimeOffset createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        DateTimeOffset editedAt = DateTimeOffset.UtcNow;
        MessageEdited @event = new()
        {
            MessageId = "msg-001",
            PreviousContent = "Original content",
            NewContent = "Updated content",
            EditedAt = editedAt,
        };
        ChatState existingState = new()
        {
            IsCreated = true,
            Name = "General",
            Messages =
            [
                new()
                {
                    MessageId = "msg-001",
                    Content = "Original content",
                    Author = "user1",
                    CreatedAt = createdAt,
                },
            ],
        };

        // Act
        ChatState result = reducer.Reduce(existingState, @event);

        // Assert
        Assert.Single(result.Messages);
        Assert.Equal("msg-001", result.Messages[0].MessageId);
        Assert.Equal("Updated content", result.Messages[0].Content);
        Assert.Equal("user1", result.Messages[0].Author);
        Assert.Equal(createdAt, result.Messages[0].CreatedAt);
        Assert.Equal(editedAt, result.Messages[0].EditedAt);
    }

    /// <summary>
    ///     Verifies that reducing when message is not in state returns unchanged state.
    /// </summary>
    [Fact]
    [AllureStep("Reduce MessageEdited when message not in state returns unchanged")]
    public void ReduceMessageEditedWhenMessageNotInStateReturnsUnchanged()
    {
        // Arrange
        MessageEditedReducer reducer = new();
        MessageEdited @event = new()
        {
            MessageId = "msg-999",
            PreviousContent = "Original",
            NewContent = "Updated",
            EditedAt = DateTimeOffset.UtcNow,
        };
        ChatState existingState = new()
        {
            IsCreated = true,
            Name = "General",
            Messages =
            [
                new()
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
        MessageEditedReducer reducer = new();
        ChatState existingState = new()
        {
            IsCreated = true,
            Name = "General",
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => reducer.Reduce(existingState, null!));
    }
}