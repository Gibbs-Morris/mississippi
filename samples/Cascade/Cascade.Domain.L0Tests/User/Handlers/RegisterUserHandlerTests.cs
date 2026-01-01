// <copyright file="RegisterUserHandlerTests.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;
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
///     Tests for <see cref="RegisterUserHandler" />.
/// </summary>
[AllureSuite("User")]
[AllureSubSuite("Handlers")]
[AllureFeature("RegisterUser")]
public sealed class RegisterUserHandlerTests
{
    /// <summary>
    ///     Verifies that registering an already registered user returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle RegisterUser when already registered")]
    public void HandleReturnsErrorWhenAlreadyRegistered()
    {
        // Arrange
        RegisterUserHandler handler = new();
        RegisterUser command = new()
        {
            UserId = "user-123",
            DisplayName = "John Doe",
        };
        UserAggregate existingState = new()
        {
            IsRegistered = true,
            UserId = "user-123",
            DisplayName = "Existing Name",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, existingState);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that registering with an empty display name returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle RegisterUser with empty display name")]
    public void HandleReturnsErrorWhenDisplayNameEmpty()
    {
        // Arrange
        RegisterUserHandler handler = new();
        RegisterUser command = new()
        {
            UserId = "user-123",
            DisplayName = string.Empty,
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that registering with an empty user ID returns an error.
    /// </summary>
    [Fact]
    [AllureStep("Handle RegisterUser with empty user ID")]
    public void HandleReturnsErrorWhenUserIdEmpty()
    {
        // Arrange
        RegisterUserHandler handler = new();
        RegisterUser command = new()
        {
            UserId = string.Empty,
            DisplayName = "John Doe",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidCommand, result.ErrorCode);
    }

    /// <summary>
    ///     Verifies that registering a new user returns a UserRegistered event.
    /// </summary>
    [Fact]
    [AllureStep("Handle RegisterUser when not registered")]
    public void HandleReturnsUserRegisteredEventWhenNotRegistered()
    {
        // Arrange
        RegisterUserHandler handler = new();
        RegisterUser command = new()
        {
            UserId = "user-123",
            DisplayName = "John Doe",
        };

        // Act
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, null);

        // Assert
        Assert.True(result.Success);
        object singleEvent = Assert.Single(result.Value!);
        UserRegistered registered = Assert.IsType<UserRegistered>(singleEvent);
        Assert.Equal("user-123", registered.UserId);
        Assert.Equal("John Doe", registered.DisplayName);
        Assert.True(registered.RegisteredAt > DateTimeOffset.UtcNow.AddMinutes(-1));
    }
}