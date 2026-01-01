// <copyright file="UserWentOfflineProjectionReducerTests.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;

using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.Projections.UserProfile;
using Cascade.Domain.Projections.UserProfile.Reducers;
using Cascade.Domain.User.Events;


namespace Cascade.Domain.L0Tests.Projections.UserProfile.Reducers;

/// <summary>
///     Unit tests for the <see cref="UserWentOfflineProjectionReducer" /> class.
/// </summary>
public sealed class UserWentOfflineProjectionReducerTests
{
    /// <summary>
    ///     Verifies that reducing a UserWentOffline event sets online status to false.
    /// </summary>
    [Fact]
    [AllureStep("Reduce UserWentOffline sets online status to false")]
    public void ReduceSetsOfflineStatus()
    {
        // Arrange
        UserWentOfflineProjectionReducer reducer = new();
        UserProfileProjection existingState = new()
        {
            UserId = "user-123",
            DisplayName = "John Doe",
            IsOnline = true,
            RegisteredAt = DateTimeOffset.UtcNow.AddDays(-1),
            LastOnlineAt = DateTimeOffset.UtcNow.AddHours(-1),
        };
        UserWentOffline evt = new()
        {
            Timestamp = DateTimeOffset.UtcNow,
        };

        // Act
        UserProfileProjection result = reducer.Reduce(existingState, evt);

        // Assert
        Assert.NotSame(existingState, result);
        Assert.False(result.IsOnline);
        Assert.Equal("user-123", result.UserId);
        Assert.Equal("John Doe", result.DisplayName);
        Assert.NotNull(result.LastOnlineAt);
    }
}