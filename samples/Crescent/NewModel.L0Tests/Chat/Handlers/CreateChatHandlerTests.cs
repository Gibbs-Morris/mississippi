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
///     Tests for <see cref="CreateChatHandler" />.
/// </summary>
[AllureParentSuite("Crescent")]
[AllureSuite("NewModel")]
[AllureSubSuite("CreateChatHandler")]
public sealed class CreateChatHandlerTests
{
    /// <summary>
    ///     Verifies that creating a chat when one already exists fails.
    /// </summary>
    [Fact]
    [AllureStep("CreateChat when chat exists fails with AlreadyExists")]
    public void CreateChatWhenChatExistsFailsWithAlreadyExists()
    {
        // Arrange
        CreateChatHandler handler = new();
        CreateChat command = new()
        {
            Name = "General",
        };
        ChatAggregate existingState = new()
        {
            IsCreated = true,
            Name = "Existing Chat",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, existingState);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.AlreadyExists, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that creating a chat with an empty name fails.
    /// </summary>
    [Fact]
    [AllureStep("CreateChat with empty name fails with InvalidCommand")]
    public void CreateChatWithEmptyNameFailsWithInvalidCommand()
    {
        // Arrange
        CreateChatHandler handler = new();
        CreateChat command = new()
        {
            Name = string.Empty,
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that creating a chat with a valid name produces a ChatCreated event.
    /// </summary>
    [Fact]
    [AllureStep("CreateChat with valid name produces ChatCreated event")]
    public void CreateChatWithValidNameProducesChatCreatedEvent()
    {
        // Arrange
        CreateChatHandler handler = new();
        CreateChat command = new()
        {
            Name = "General",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
        ChatCreated? @event = Assert.IsType<ChatCreated>(result.Value[0]);
        Assert.Equal("General", @event.Name);
    }
}