// <copyright file="UserRegisteredReducerTests.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.User;
using Cascade.Domain.User.Events;
using Cascade.Domain.User.Reducers;

using Xunit;


namespace Cascade.Domain.L0Tests.User.Reducers;

/// <summary>
///     Tests for <see cref="UserRegisteredReducer" />.
/// </summary>
[AllureSuite("User")]
[AllureSubSuite("Reducers")]
[AllureFeature("UserRegistered")]
public sealed class UserRegisteredReducerTests
{
    /// <summary>
    ///     Verifies that reducing a UserRegistered event creates a registered user state.
    /// </summary>
    [Fact]
    [AllureStep("Reduce UserRegistered creates registered state")]
    public void ReduceCreatesRegisteredState()
    {
        // Arrange
        UserRegisteredReducer reducer = new();
        DateTimeOffset registeredAt = DateTimeOffset.UtcNow;
        UserRegistered evt = new()
        {
            UserId = "user-123",
            DisplayName = "John Doe",
            RegisteredAt = registeredAt,
        };

        // Act
        UserState result = reducer.Reduce(null!, evt);

        // Assert
        Assert.True(result.IsRegistered);
        Assert.Equal("user-123", result.UserId);
        Assert.Equal("John Doe", result.DisplayName);
        Assert.Equal(registeredAt, result.RegisteredAt);
        Assert.False(result.IsOnline);
        Assert.Empty(result.ChannelIds);
    }

    /// <summary>
    ///     Verifies that reducing a UserRegistered event overwrites existing state.
    /// </summary>
    [Fact]
    [AllureStep("Reduce UserRegistered overwrites existing state")]
    public void ReduceOverwritesExistingState()
    {
        // Arrange
        UserRegisteredReducer reducer = new();
        DateTimeOffset registeredAt = DateTimeOffset.UtcNow;
        UserRegistered evt = new()
        {
            UserId = "user-456",
            DisplayName = "Jane Doe",
            RegisteredAt = registeredAt,
        };
        UserState existingState = new()
        {
            IsRegistered = true,
            UserId = "old-user",
            DisplayName = "Old Name",
        };

        // Act
        UserState result = reducer.Reduce(existingState, evt);

        // Assert
        Assert.True(result.IsRegistered);
        Assert.Equal("user-456", result.UserId);
        Assert.Equal("Jane Doe", result.DisplayName);
        Assert.Equal(registeredAt, result.RegisteredAt);
    }
}
