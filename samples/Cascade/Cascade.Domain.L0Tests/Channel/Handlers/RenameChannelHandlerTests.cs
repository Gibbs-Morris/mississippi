using System.Collections.Generic;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.Channel;
using Cascade.Domain.Channel.Commands;
using Cascade.Domain.Channel.Events;
using Cascade.Domain.Channel.Handlers;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.L0Tests.Channel.Handlers;

/// <summary>
///     Tests for <see cref="RenameChannelHandler" />.
/// </summary>
[AllureSuite("Channel")]
[AllureSubSuite("Handlers")]
[AllureFeature("RenameChannel")]
public sealed class RenameChannelHandlerTests
{
    /// <summary>
    ///     Verifies that renaming a channel returns a ChannelRenamed event.
    /// </summary>
    [Fact]
    [AllureStep("Handle RenameChannel when created")]
    public void HandleReturnsChannelRenamedEventWhenCreated()
    {
        // Arrange
        RenameChannelHandler handler = new();
        RenameChannel command = new()
        {
            NewName = "New Name",
        };
        ChannelAggregate state = new()
        {
            IsCreated = true,
            ChannelId = "channel-1",
            Name = "Old Name",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        // Assert
        Assert.True(result.Success);
        object singleEvent = Assert.Single(result.Value!);
        ChannelRenamed renamed = Assert.IsType<ChannelRenamed>(singleEvent);
        Assert.Equal("Old Name", renamed.OldName);
        Assert.Equal("New Name", renamed.NewName);
    }

    /// <summary>
    ///     Verifies that renaming an archived channel returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle RenameChannel when archived")]
    public void HandleReturnsErrorWhenArchived()
    {
        // Arrange
        RenameChannelHandler handler = new();
        RenameChannel command = new()
        {
            NewName = "New Name",
        };
        ChannelAggregate state = new()
        {
            IsCreated = true,
            ChannelId = "channel-1",
            Name = "Old Name",
            IsArchived = true,
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that renaming with empty name returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle RenameChannel with empty name")]
    public void HandleReturnsErrorWhenNewNameEmpty()
    {
        // Arrange
        RenameChannelHandler handler = new();
        RenameChannel command = new()
        {
            NewName = string.Empty,
        };
        ChannelAggregate state = new()
        {
            IsCreated = true,
            ChannelId = "channel-1",
            Name = "Old Name",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that renaming a non-existent channel returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle RenameChannel when not created")]
    public void HandleReturnsErrorWhenNotCreated()
    {
        // Arrange
        RenameChannelHandler handler = new();
        RenameChannel command = new()
        {
            NewName = "New Name",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }
}