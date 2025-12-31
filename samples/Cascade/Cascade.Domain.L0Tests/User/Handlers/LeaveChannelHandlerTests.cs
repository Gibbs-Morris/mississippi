// <copyright file="LeaveChannelHandlerTests.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Collections.Immutable;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.User;
using Cascade.Domain.User.Commands;
using Cascade.Domain.User.Events;
using Cascade.Domain.User.Handlers;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Xunit;


namespace Cascade.Domain.L0Tests.User.Handlers;

/// <summary>
///     Tests for <see cref="LeaveChannelHandler" />.
/// </summary>
[AllureSuite("User")]
[AllureSubSuite("Handlers")]
[AllureFeature("LeaveChannel")]
public sealed class LeaveChannelHandlerTests
{
    /// <summary>
    ///     Verifies that leaving a channel returns a UserLeftChannel event.
    /// </summary>
    [Fact]
    [AllureStep("Handle LeaveChannel when a member")]
    public void HandleReturnsUserLeftChannelEventWhenMember()
    {
        // Arrange
        LeaveChannelHandler handler = new();
        LeaveChannel command = new()
        {
            ChannelId = "channel-1",
        };
        UserState state = new()
        {
            IsRegistered = true,
            UserId = "user-123",
            DisplayName = "John",
            ChannelIds = ImmutableHashSet.Create("channel-1"),
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        // Assert
        Assert.True(result.Success);
        object singleEvent = Assert.Single(result.Value!);
        UserLeftChannel left = Assert.IsType<UserLeftChannel>(singleEvent);
        Assert.Equal("channel-1", left.ChannelId);
    }

    /// <summary>
    ///     Verifies that leaving a channel you're not in returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle LeaveChannel when not a member")]
    public void HandleReturnsErrorWhenNotMember()
    {
        // Arrange
        LeaveChannelHandler handler = new();
        LeaveChannel command = new()
        {
            ChannelId = "channel-1",
        };
        UserState state = new()
        {
            IsRegistered = true,
            UserId = "user-123",
            DisplayName = "John",
            ChannelIds = ImmutableHashSet<string>.Empty,
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that leaving a channel when not registered returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle LeaveChannel when not registered")]
    public void HandleReturnsErrorWhenNotRegistered()
    {
        // Arrange
        LeaveChannelHandler handler = new();
        LeaveChannel command = new()
        {
            ChannelId = "channel-1",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that leaving a channel with empty channel ID returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle LeaveChannel with empty channel ID")]
    public void HandleReturnsErrorWhenChannelIdEmpty()
    {
        // Arrange
        LeaveChannelHandler handler = new();
        LeaveChannel command = new()
        {
            ChannelId = string.Empty,
        };
        UserState state = new()
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
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
    }
}
