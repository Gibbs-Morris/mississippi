using System;
using System.Collections.Generic;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.Aggregates.Channel;
using Cascade.Domain.Aggregates.Channel.Commands;
using Cascade.Domain.Aggregates.Channel.Events;
using Cascade.Domain.Aggregates.Channel.Handlers;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.L0Tests.Channel.Handlers;

/// <summary>
///     Tests for <see cref="CreateChannelHandler" />.
/// </summary>
[AllureParentSuite("Cascade")]
[AllureSuite("Channel")]
[AllureSubSuite("Handlers")]
[AllureFeature("CreateChannel")]
public sealed class CreateChannelHandlerTests
{
    /// <summary>
    ///     Verifies that creating a new channel returns a ChannelCreated event.
    /// </summary>
    [Fact]
    [AllureStep("Handle CreateChannel when not created")]
    public void HandleReturnsChannelCreatedEventWhenNotCreated()
    {
        // Arrange
        CreateChannelHandler handler = new();
        CreateChannel command = new()
        {
            ChannelId = "channel-1",
            Name = "General",
            CreatedBy = "user-123",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        // Assert
        Assert.True(result.Success);
        object singleEvent = Assert.Single(result.Value!);
        ChannelCreated created = Assert.IsType<ChannelCreated>(singleEvent);
        Assert.Equal("channel-1", created.ChannelId);
        Assert.Equal("General", created.Name);
        Assert.Equal("user-123", created.CreatedBy);
        Assert.True(created.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    /// <summary>
    ///     Verifies that creating an already-created channel returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle CreateChannel when already created")]
    public void HandleReturnsErrorWhenAlreadyCreated()
    {
        // Arrange
        CreateChannelHandler handler = new();
        CreateChannel command = new()
        {
            ChannelId = "channel-1",
            Name = "General",
            CreatedBy = "user-123",
        };
        ChannelAggregate existingState = new()
        {
            IsCreated = true,
            ChannelId = "channel-1",
            Name = "Existing",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, existingState);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that creating a channel with empty channel ID returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle CreateChannel with empty channel ID")]
    public void HandleReturnsErrorWhenChannelIdEmpty()
    {
        // Arrange
        CreateChannelHandler handler = new();
        CreateChannel command = new()
        {
            ChannelId = string.Empty,
            Name = "General",
            CreatedBy = "user-123",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that creating a channel with empty createdBy returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle CreateChannel with empty createdBy")]
    public void HandleReturnsErrorWhenCreatedByEmpty()
    {
        // Arrange
        CreateChannelHandler handler = new();
        CreateChannel command = new()
        {
            ChannelId = "channel-1",
            Name = "General",
            CreatedBy = string.Empty,
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that creating a channel with empty name returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle CreateChannel with empty name")]
    public void HandleReturnsErrorWhenNameEmpty()
    {
        // Arrange
        CreateChannelHandler handler = new();
        CreateChannel command = new()
        {
            ChannelId = "channel-1",
            Name = string.Empty,
            CreatedBy = "user-123",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
    }
}