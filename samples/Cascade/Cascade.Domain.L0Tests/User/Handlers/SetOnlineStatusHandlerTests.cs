// <copyright file="SetOnlineStatusHandlerTests.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System.Collections.Generic;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.User;
using Cascade.Domain.User.Commands;
using Cascade.Domain.User.Events;
using Cascade.Domain.User.Handlers;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.L0Tests.User.Handlers;

/// <summary>
///     Tests for <see cref="SetOnlineStatusHandler" />.
/// </summary>
[AllureSuite("User")]
[AllureSubSuite("Handlers")]
[AllureFeature("SetOnlineStatus")]
public sealed class SetOnlineStatusHandlerTests
{
    /// <summary>
    ///     Verifies that setting same status returns empty events list.
    /// </summary>
    [Fact]
    [AllureStep("Handle SetOnlineStatus when already in same state")]
    public void HandleReturnsEmptyEventsWhenAlreadySameStatus()
    {
        // Arrange
        SetOnlineStatusHandler handler = new();
        SetOnlineStatus command = new()
        {
            IsOnline = true,
        };
        UserState state = new()
        {
            IsRegistered = true,
            UserId = "user-123",
            DisplayName = "John",
            IsOnline = true,
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Value!);
    }

    /// <summary>
    ///     Verifies that setting online status for an unregistered user returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle SetOnlineStatus when not registered")]
    public void HandleReturnsErrorWhenNotRegistered()
    {
        // Arrange
        SetOnlineStatusHandler handler = new();
        SetOnlineStatus command = new()
        {
            IsOnline = true,
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that setting online status to false returns a UserWentOffline event.
    /// </summary>
    [Fact]
    [AllureStep("Handle SetOnlineStatus when going offline")]
    public void HandleReturnsUserWentOfflineEventWhenGoingOffline()
    {
        // Arrange
        SetOnlineStatusHandler handler = new();
        SetOnlineStatus command = new()
        {
            IsOnline = false,
        };
        UserState state = new()
        {
            IsRegistered = true,
            UserId = "user-123",
            DisplayName = "John",
            IsOnline = true,
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        // Assert
        Assert.True(result.Success);
        object singleEvent = Assert.Single(result.Value!);
        Assert.IsType<UserWentOffline>(singleEvent);
    }

    /// <summary>
    ///     Verifies that setting online status to true returns a UserWentOnline event.
    /// </summary>
    [Fact]
    [AllureStep("Handle SetOnlineStatus when going online")]
    public void HandleReturnsUserWentOnlineEventWhenGoingOnline()
    {
        // Arrange
        SetOnlineStatusHandler handler = new();
        SetOnlineStatus command = new()
        {
            IsOnline = true,
        };
        UserState state = new()
        {
            IsRegistered = true,
            UserId = "user-123",
            DisplayName = "John",
            IsOnline = false,
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);

        // Assert
        Assert.True(result.Success);
        object singleEvent = Assert.Single(result.Value!);
        Assert.IsType<UserWentOnline>(singleEvent);
    }
}