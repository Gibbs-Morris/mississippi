using System;
using System.Collections.Immutable;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.Projections.UserProfile;
using Cascade.Domain.Projections.UserProfile.Reducers;
using Cascade.Domain.User.Events;


namespace Cascade.Domain.L0Tests.Projections.UserProfile.Reducers;

/// <summary>
///     Unit tests for the <see cref="UserRegisteredProjectionReducer" /> class.
/// </summary>
[AllureParentSuite("Cascade")]
[AllureSuite("Core")]
[AllureSubSuite("Unit")]
public sealed class UserRegisteredProjectionReducerTests
{
    /// <summary>
    ///     Verifies that reducing a UserRegistered event creates a new projection.
    /// </summary>
    [Fact]
    [AllureStep("Reduce UserRegistered creates initial projection")]
    public void ReduceCreatesInitialProjection()
    {
        // Arrange
        UserRegisteredProjectionReducer reducer = new();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        UserRegistered evt = new()
        {
            UserId = "user-123",
            DisplayName = "John Doe",
            RegisteredAt = now,
        };

        // Act
        UserProfileProjection result = reducer.Reduce(null!, evt);

        // Assert
        Assert.Equal("user-123", result.UserId);
        Assert.Equal("John Doe", result.DisplayName);
        Assert.False(result.IsOnline);
        Assert.Equal(now, result.RegisteredAt);
        Assert.Null(result.LastOnlineAt);
        Assert.Equal(0, result.ChannelCount);
        Assert.Empty(result.ChannelIds);
    }

    /// <summary>
    ///     Verifies that reducing a UserRegistered event with existing state creates a new projection.
    /// </summary>
    [Fact]
    [AllureStep("Reduce UserRegistered replaces existing state")]
    public void ReduceReplacesExistingState()
    {
        // Arrange
        UserRegisteredProjectionReducer reducer = new();
        DateTimeOffset oldTime = DateTimeOffset.UtcNow.AddDays(-1);
        DateTimeOffset newTime = DateTimeOffset.UtcNow;
        UserProfileProjection existingState = new()
        {
            UserId = "old-user",
            DisplayName = "Old Name",
            IsOnline = true,
            RegisteredAt = oldTime,
            ChannelCount = 5,
            ChannelIds = ImmutableList.Create("channel-1", "channel-2"),
        };
        UserRegistered evt = new()
        {
            UserId = "user-123",
            DisplayName = "John Doe",
            RegisteredAt = newTime,
        };

        // Act
        UserProfileProjection result = reducer.Reduce(existingState, evt);

        // Assert
        Assert.Equal("user-123", result.UserId);
        Assert.Equal("John Doe", result.DisplayName);
        Assert.False(result.IsOnline);
        Assert.Equal(newTime, result.RegisteredAt);
        Assert.Null(result.LastOnlineAt);
        Assert.Equal(0, result.ChannelCount);
    }
}