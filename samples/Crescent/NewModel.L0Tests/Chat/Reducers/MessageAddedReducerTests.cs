// <copyright file="MessageAddedReducerTests.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;
using System.Collections.Immutable;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Crescent.NewModel.Chat;
using Crescent.NewModel.Chat.Events;
using Crescent.NewModel.Chat.Reducers;


namespace Crescent.NewModel.L0Tests.Chat.Reducers;

/// <summary>
///     Tests for <see cref="MessageAddedReducer" />.
/// </summary>
[AllureParentSuite("Crescent")]
[AllureSuite("NewModel")]
[AllureSubSuite("MessageAddedReducer")]
public sealed class MessageAddedReducerTests
{
    /// <summary>
    ///     Verifies that reducing a MessageAdded event adds the message to state.
    /// </summary>
    [Fact]
    [AllureStep("Reduce MessageAdded event adds message to state")]
    public void ReduceMessageAddedEventAddsMessageToState()
    {
        // Arrange
        MessageAddedReducer reducer = new();
        DateTimeOffset createdAt = DateTimeOffset.UtcNow;
        MessageAdded @event = new()
        {
            MessageId = "msg-001",
            Content = "Hello, world!",
            Author = "user1",
            CreatedAt = createdAt,
        };
        ChatState existingState = new()
        {
            IsCreated = true,
            Name = "General",
        };

        // Act
        ChatState result = reducer.Reduce(existingState, @event);

        // Assert
        Assert.Single(result.Messages);
        Assert.Equal("msg-001", result.Messages[0].MessageId);
        Assert.Equal("Hello, world!", result.Messages[0].Content);
        Assert.Equal("user1", result.Messages[0].Author);
        Assert.Equal(createdAt, result.Messages[0].CreatedAt);
        Assert.Equal(1, result.TotalMessageCount);
    }

    /// <summary>
    ///     Verifies that the reducer maintains only the last 50 messages.
    /// </summary>
    [Fact]
    [AllureStep("Reduce MessageAdded maintains only last 50 messages")]
    public void ReduceMessageAddedMaintainsOnlyLast50Messages()
    {
        // Arrange
        MessageAddedReducer reducer = new();
        ImmutableList<ChatMessage>.Builder messagesBuilder = ImmutableList.CreateBuilder<ChatMessage>();
        for (int i = 0; i < 50; i++)
        {
            messagesBuilder.Add(
                new()
                {
                    MessageId = $"msg-{i:D3}",
                    Content = $"Message {i}",
                    Author = "user1",
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-50 + i),
                });
        }

        ChatState existingState = new()
        {
            IsCreated = true,
            Name = "General",
            Messages = messagesBuilder.ToImmutable(),
            TotalMessageCount = 50,
        };
        MessageAdded @event = new()
        {
            MessageId = "msg-050",
            Content = "New message 50",
            Author = "user1",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        // Act
        ChatState result = reducer.Reduce(existingState, @event);

        // Assert
        Assert.Equal(50, result.Messages.Count);
        Assert.Equal("msg-001", result.Messages[0].MessageId); // First message should now be msg-001
        Assert.Equal("msg-050", result.Messages[49].MessageId); // Last message should be the new one
        Assert.Equal(51, result.TotalMessageCount);
    }

    /// <summary>
    ///     Verifies that reducing with a null event throws ArgumentNullException.
    /// </summary>
    [Fact]
    [AllureStep("Reduce with null event throws ArgumentNullException")]
    public void ReduceNullEventThrowsArgumentNullException()
    {
        // Arrange
        MessageAddedReducer reducer = new();
        ChatState existingState = new()
        {
            IsCreated = true,
            Name = "General",
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => reducer.Reduce(existingState, null!));
    }
}