using System.Collections.Generic;
using System.Collections.Immutable;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.Aggregates.User;
using Cascade.Domain.Aggregates.User.Commands;
using Cascade.Domain.Aggregates.User.Events;
using Cascade.Domain.Aggregates.User.Handlers;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.L0Tests.User.Handlers;

/// <summary>
///     Tests for <see cref="JoinChannelHandler" />.
/// </summary>
[AllureParentSuite("Cascade")]
[AllureSuite("User")]
[AllureSubSuite("Handlers")]
[AllureFeature("JoinChannel")]
public sealed class JoinChannelHandlerTests
{
    /// <summary>
    ///     Verifies that joining an already-joined channel returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle JoinChannel when already a member")]
    public void HandleReturnsErrorWhenAlreadyMember()
    {
        // Arrange
        JoinChannelHandler handler = new();
        JoinChannel command = new()
        {
            ChannelId = "channel-1",
            ChannelName = "General",
        };
        UserAggregate state = new()
        {
            IsRegistered = true,
            UserId = "user-123",
            DisplayName = "John",
            ChannelIds = ImmutableHashSet.Create("channel-1"),
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that joining a channel with empty channel ID returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle JoinChannel with empty channel ID")]
    public void HandleReturnsErrorWhenChannelIdEmpty()
    {
        // Arrange
        JoinChannelHandler handler = new();
        JoinChannel command = new()
        {
            ChannelId = string.Empty,
            ChannelName = "General",
        };
        UserAggregate state = new()
        {
            IsRegistered = true,
            UserId = "user-123",
            DisplayName = "John",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that joining a channel when not registered returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle JoinChannel when not registered")]
    public void HandleReturnsErrorWhenNotRegistered()
    {
        // Arrange
        JoinChannelHandler handler = new();
        JoinChannel command = new()
        {
            ChannelId = "channel-1",
            ChannelName = "General",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that joining a channel returns a UserJoinedChannel event.
    /// </summary>
    [Fact]
    [AllureStep("Handle JoinChannel when not already a member")]
    public void HandleReturnsUserJoinedChannelEventWhenNotMember()
    {
        // Arrange
        JoinChannelHandler handler = new();
        JoinChannel command = new()
        {
            ChannelId = "channel-1",
            ChannelName = "General",
        };
        UserAggregate state = new()
        {
            IsRegistered = true,
            UserId = "user-123",
            DisplayName = "John",
            ChannelIds = ImmutableHashSet<string>.Empty,
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        // Assert
        Assert.True(result.Success);
        object singleEvent = Assert.Single(result.Value!);
        UserJoinedChannel joined = Assert.IsType<UserJoinedChannel>(singleEvent);
        Assert.Equal("channel-1", joined.ChannelId);
    }
}