// <copyright file="EditMessageHandlerTests.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Crescent.NewModel.Chat;
using Crescent.NewModel.Chat.Commands;
using Crescent.NewModel.Chat.Events;
using Crescent.NewModel.Chat.Handlers;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Xunit;


namespace Crescent.NewModel.L0Tests.Chat.Handlers;

/// <summary>
///     Tests for <see cref="EditMessageHandler" />.
/// </summary>
[AllureParentSuite("Crescent")]
[AllureSuite("NewModel")]
[AllureSubSuite("EditMessageHandler")]
public sealed class EditMessageHandlerTests
{
    /// <summary>
    ///     Verifies that editing a message produces a MessageEdited event.
    /// </summary>
    [Fact]
    [AllureStep("EditMessage with valid data produces MessageEdited event")]
    public void EditMessageWithValidDataProducesMessageEditedEvent()
    {
        // Arrange
        EditMessageHandler handler = new();
        EditMessage command = new()
        {
            MessageId = "msg-001",
            NewContent = "Updated content",
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
                    Content = "Original content",
                    Author = "user1",
                    CreatedAt = DateTimeOffset.UtcNow,
                },
            ],
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, existingState);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
        MessageEdited? @event = Assert.IsType<MessageEdited>(result.Value[0]);
        Assert.Equal("msg-001", @event.MessageId);
        Assert.Equal("Original content", @event.PreviousContent);
        Assert.Equal("Updated content", @event.NewContent);
    }

    /// <summary>
    ///     Verifies that editing a message when chat does not exist fails.
    /// </summary>
    [Fact]
    [AllureStep("EditMessage when chat does not exist fails with NotFound")]
    public void EditMessageWhenChatDoesNotExistFailsWithNotFound()
    {
        // Arrange
        EditMessageHandler handler = new();
        EditMessage command = new()
        {
            MessageId = "msg-001",
            NewContent = "Updated content",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.NotFound, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that editing a non-existent message fails.
    /// </summary>
    [Fact]
    [AllureStep("EditMessage when message does not exist fails with NotFound")]
    public void EditMessageWhenMessageDoesNotExistFailsWithNotFound()
    {
        // Arrange
        EditMessageHandler handler = new();
        EditMessage command = new()
        {
            MessageId = "msg-999",
            NewContent = "Updated content",
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
                    Content = "Original content",
                    Author = "user1",
                    CreatedAt = DateTimeOffset.UtcNow,
                },
            ],
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, existingState);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.NotFound, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that editing with the same content produces no events.
    /// </summary>
    [Fact]
    [AllureStep("EditMessage with same content produces no events")]
    public void EditMessageWithSameContentProducesNoEvents()
    {
        // Arrange
        EditMessageHandler handler = new();
        EditMessage command = new()
        {
            MessageId = "msg-001",
            NewContent = "Same content",
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
                    Content = "Same content",
                    Author = "user1",
                    CreatedAt = DateTimeOffset.UtcNow,
                },
            ],
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, existingState);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    /// <summary>
    ///     Verifies that editing with empty content fails.
    /// </summary>
    [Fact]
    [AllureStep("EditMessage with empty content fails with InvalidCommand")]
    public void EditMessageWithEmptyContentFailsWithInvalidCommand()
    {
        // Arrange
        EditMessageHandler handler = new();
        EditMessage command = new()
        {
            MessageId = "msg-001",
            NewContent = string.Empty,
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
                    Content = "Original content",
                    Author = "user1",
                    CreatedAt = DateTimeOffset.UtcNow,
                },
            ],
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, existingState);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
    }
}
