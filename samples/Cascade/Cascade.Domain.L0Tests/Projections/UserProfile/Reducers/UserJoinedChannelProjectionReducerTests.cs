// <copyright file="UserJoinedChannelProjectionReducerTests.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;
using System.Collections.Immutable;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.Projections.UserProfile;
using Cascade.Domain.Projections.UserProfile.Reducers;
using Cascade.Domain.User.Events;

using Xunit;


namespace Cascade.Domain.L0Tests.Projections.UserProfile.Reducers;

/// <summary>
///     Unit tests for the <see cref="UserJoinedChannelProjectionReducer" /> class.
/// </summary>
public sealed class UserJoinedChannelProjectionReducerTests
{
    /// <summary>
    ///     Verifies that reducing a UserJoinedChannel event adds the channel.
    /// </summary>
    [Fact]
    [AllureStep("Reduce UserJoinedChannel adds channel")]
    public void ReduceAddsChannel()
    {
        // Arrange
        UserJoinedChannelProjectionReducer reducer = new();
        UserProfileProjection existingState = new()
        {
            UserId = "user-123",
            DisplayName = "John Doe",
            IsOnline = true,
            ChannelCount = 0,
            ChannelIds = ImmutableList<string>.Empty,
        };
        UserJoinedChannel evt = new()
        {
            ChannelId = "channel-1",
            JoinedAt = DateTimeOffset.UtcNow,
        };

        // Act
        UserProfileProjection result = reducer.Reduce(existingState, evt);

        // Assert
        Assert.NotSame(existingState, result);
        Assert.Equal(1, result.ChannelCount);
        Assert.Single(result.ChannelIds);
        Assert.Contains("channel-1", result.ChannelIds);
    }

    /// <summary>
    ///     Verifies that reducing a UserJoinedChannel event increments channel count.
    /// </summary>
    [Fact]
    [AllureStep("Reduce UserJoinedChannel increments channel count")]
    public void ReduceIncrementsChannelCount()
    {
        // Arrange
        UserJoinedChannelProjectionReducer reducer = new();
        UserProfileProjection existingState = new()
        {
            UserId = "user-123",
            DisplayName = "John Doe",
            ChannelCount = 2,
            ChannelIds = ImmutableList.Create("channel-1", "channel-2"),
        };
        UserJoinedChannel evt = new()
        {
            ChannelId = "channel-3",
            JoinedAt = DateTimeOffset.UtcNow,
        };

        // Act
        UserProfileProjection result = reducer.Reduce(existingState, evt);

        // Assert
        Assert.Equal(3, result.ChannelCount);
        Assert.Equal(3, result.ChannelIds.Count);
        Assert.Contains("channel-3", result.ChannelIds);
    }

    /// <summary>
    ///     Verifies that reducing a UserJoinedChannel event with duplicate channel is idempotent.
    /// </summary>
    [Fact]
    [AllureStep("Reduce UserJoinedChannel ignores duplicate channel")]
    public void ReduceIgnoresDuplicateChannel()
    {
        // Arrange
        UserJoinedChannelProjectionReducer reducer = new();
        UserProfileProjection existingState = new()
        {
            UserId = "user-123",
            DisplayName = "John Doe",
            ChannelCount = 2,
            ChannelIds = ImmutableList.Create("channel-1", "channel-2"),
        };
        UserJoinedChannel evt = new()
        {
            ChannelId = "channel-1",
            JoinedAt = DateTimeOffset.UtcNow,
        };

        // Act
        UserProfileProjection result = reducer.Reduce(existingState, evt);

        // Assert - Returns new instance but with same values
        Assert.NotSame(existingState, result);
        Assert.Equal(2, result.ChannelCount);
        Assert.Equal(2, result.ChannelIds.Count);
    }
}
