// <copyright file="UserWentOnlineProjectionReducerTests.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;

using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.Projections.UserProfile;
using Cascade.Domain.Projections.UserProfile.Reducers;
using Cascade.Domain.User.Events;


namespace Cascade.Domain.L0Tests.Projections.UserProfile.Reducers;

/// <summary>
///     Unit tests for the <see cref="UserWentOnlineProjectionReducer" /> class.
/// </summary>
public sealed class UserWentOnlineProjectionReducerTests
{
    /// <summary>
    ///     Verifies that reducing a UserWentOnline event sets online status to true.
    /// </summary>
    [Fact]
    [AllureStep("Reduce UserWentOnline sets online status to true")]
    public void ReduceSetsOnlineStatus()
    {
        // Arrange
        UserWentOnlineProjectionReducer reducer = new();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        UserProfileProjection existingState = new()
        {
            UserId = "user-123",
            DisplayName = "John Doe",
            IsOnline = false,
            RegisteredAt = DateTimeOffset.UtcNow.AddDays(-1),
            LastOnlineAt = null,
        };
        UserWentOnline evt = new()
        {
            Timestamp = now,
        };

        // Act
        UserProfileProjection result = reducer.Reduce(existingState, evt);

        // Assert
        Assert.NotSame(existingState, result);
        Assert.True(result.IsOnline);
        Assert.Equal(now, result.LastOnlineAt);
        Assert.Equal("user-123", result.UserId);
        Assert.Equal("John Doe", result.DisplayName);
    }

    /// <summary>
    ///     Verifies that reducing a UserWentOnline event updates the last online timestamp.
    /// </summary>
    [Fact]
    [AllureStep("Reduce UserWentOnline updates last online timestamp")]
    public void ReduceUpdatesLastOnlineTimestamp()
    {
        // Arrange
        UserWentOnlineProjectionReducer reducer = new();
        DateTimeOffset oldTime = DateTimeOffset.UtcNow.AddHours(-1);
        DateTimeOffset newTime = DateTimeOffset.UtcNow;
        UserProfileProjection existingState = new()
        {
            UserId = "user-123",
            DisplayName = "John Doe",
            IsOnline = false,
            RegisteredAt = DateTimeOffset.UtcNow.AddDays(-1),
            LastOnlineAt = oldTime,
        };
        UserWentOnline evt = new()
        {
            Timestamp = newTime,
        };

        // Act
        UserProfileProjection result = reducer.Reduce(existingState, evt);

        // Assert
        Assert.Equal(newTime, result.LastOnlineAt);
        Assert.True(result.IsOnline);
    }
}