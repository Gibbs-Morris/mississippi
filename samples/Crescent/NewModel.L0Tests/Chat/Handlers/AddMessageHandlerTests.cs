// <copyright file="AddMessageHandlerTests.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System.Collections.Generic;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Crescent.NewModel.Chat;
using Crescent.NewModel.Chat.Commands;
using Crescent.NewModel.Chat.Events;
using Crescent.NewModel.Chat.Handlers;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Crescent.NewModel.L0Tests.Chat.Handlers;

/// <summary>
///     Tests for <see cref="AddMessageHandler" />.
/// </summary>
[AllureParentSuite("Crescent")]
[AllureSuite("NewModel")]
[AllureSubSuite("AddMessageHandler")]
public sealed class AddMessageHandlerTests
{
    /// <summary>
    ///     Verifies that adding a message when chat does not exist fails.
    /// </summary>
    [Fact]
    [AllureStep("AddMessage when chat does not exist fails with NotFound")]
    public void AddMessageWhenChatDoesNotExistFailsWithNotFound()
    {
        // Arrange
        AddMessageHandler handler = new();
        AddMessage command = new()
        {
            MessageId = "msg-001",
            Content = "Hello!",
            Author = "user1",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.NotFound, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that adding a message with empty author fails.
    /// </summary>
    [Fact]
    [AllureStep("AddMessage with empty author fails with InvalidCommand")]
    public void AddMessageWithEmptyAuthorFailsWithInvalidCommand()
    {
        // Arrange
        AddMessageHandler handler = new();
        AddMessage command = new()
        {
            MessageId = "msg-001",
            Content = "Hello!",
            Author = string.Empty,
        };
        ChatAggregate existingState = new()
        {
            IsCreated = true,
            Name = "General",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, existingState);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that adding a message with empty content fails.
    /// </summary>
    [Fact]
    [AllureStep("AddMessage with empty content fails with InvalidCommand")]
    public void AddMessageWithEmptyContentFailsWithInvalidCommand()
    {
        // Arrange
        AddMessageHandler handler = new();
        AddMessage command = new()
        {
            MessageId = "msg-001",
            Content = string.Empty,
            Author = "user1",
        };
        ChatAggregate existingState = new()
        {
            IsCreated = true,
            Name = "General",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, existingState);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that adding a message with empty message ID fails.
    /// </summary>
    [Fact]
    [AllureStep("AddMessage with empty message ID fails with InvalidCommand")]
    public void AddMessageWithEmptyMessageIdFailsWithInvalidCommand()
    {
        // Arrange
        AddMessageHandler handler = new();
        AddMessage command = new()
        {
            MessageId = string.Empty,
            Content = "Hello!",
            Author = "user1",
        };
        ChatAggregate existingState = new()
        {
            IsCreated = true,
            Name = "General",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, existingState);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that adding a message produces a MessageAdded event.
    /// </summary>
    [Fact]
    [AllureStep("AddMessage with valid data produces MessageAdded event")]
    public void AddMessageWithValidDataProducesMessageAddedEvent()
    {
        // Arrange
        AddMessageHandler handler = new();
        AddMessage command = new()
        {
            MessageId = "msg-001",
            Content = "Hello, world!",
            Author = "user1",
        };
        ChatAggregate existingState = new()
        {
            IsCreated = true,
            Name = "General",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, existingState);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
        MessageAdded? @event = Assert.IsType<MessageAdded>(result.Value[0]);
        Assert.Equal("msg-001", @event.MessageId);
        Assert.Equal("Hello, world!", @event.Content);
        Assert.Equal("user1", @event.Author);
    }
}