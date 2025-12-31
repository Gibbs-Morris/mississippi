// <copyright file="ChannelMembershipReducerTests.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;
using System.Collections.Immutable;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.User;
using Cascade.Domain.User.Events;
using Cascade.Domain.User.Reducers;

using Xunit;


namespace Cascade.Domain.L0Tests.User.Reducers;

/// <summary>
///     Tests for <see cref="UserJoinedChannelReducer" /> and <see cref="UserLeftChannelReducer" />.
/// </summary>
[AllureSuite("User")]
[AllureSubSuite("Reducers")]
[AllureFeature("ChannelMembership")]
public sealed class ChannelMembershipReducerTests
{
    /// <summary>
    ///     Verifies that reducing a UserJoinedChannel event adds the channel.
    /// </summary>
    [Fact]
    [AllureStep("Reduce UserJoinedChannel adds channel")]
    public void ReduceUserJoinedChannelAddsChannel()
    {
        // Arrange
        UserJoinedChannelReducer reducer = new();
        UserJoinedChannel evt = new()
        {
            ChannelId = "channel-1",
            JoinedAt = DateTimeOffset.UtcNow,
        };
        UserState state = new()
        {
            IsRegistered = true,
            UserId = "user-123",
            DisplayName = "John",
            ChannelIds = ImmutableHashSet<string>.Empty,
        };

        // Act
        UserState result = reducer.Reduce(state, evt);

        // Assert
        Assert.Contains("channel-1", result.ChannelIds);
        Assert.Single(result.ChannelIds);
    }

    /// <summary>
    ///     Verifies that reducing a UserJoinedChannel event with existing channels adds to set.
    /// </summary>
    [Fact]
    [AllureStep("Reduce UserJoinedChannel with existing channels")]
    public void ReduceUserJoinedChannelWithExistingChannels()
    {
        // Arrange
        UserJoinedChannelReducer reducer = new();
        UserJoinedChannel evt = new()
        {
            ChannelId = "channel-2",
            JoinedAt = DateTimeOffset.UtcNow,
        };
        UserState state = new()
        {
            IsRegistered = true,
            UserId = "user-123",
            DisplayName = "John",
            ChannelIds = ImmutableHashSet.Create("channel-1"),
        };

        // Act
        UserState result = reducer.Reduce(state, evt);

        // Assert
        Assert.Contains("channel-1", result.ChannelIds);
        Assert.Contains("channel-2", result.ChannelIds);
        Assert.Equal(2, result.ChannelIds.Count);
    }

    /// <summary>
    ///     Verifies that reducing a UserLeftChannel event removes the channel.
    /// </summary>
    [Fact]
    [AllureStep("Reduce UserLeftChannel removes channel")]
    public void ReduceUserLeftChannelRemovesChannel()
    {
        // Arrange
        UserLeftChannelReducer reducer = new();
        UserLeftChannel evt = new()
        {
            ChannelId = "channel-1",
            LeftAt = DateTimeOffset.UtcNow,
        };
        UserState state = new()
        {
            IsRegistered = true,
            UserId = "user-123",
            DisplayName = "John",
            ChannelIds = ImmutableHashSet.Create("channel-1", "channel-2"),
        };

        // Act
        UserState result = reducer.Reduce(state, evt);

        // Assert
        Assert.DoesNotContain("channel-1", result.ChannelIds);
        Assert.Contains("channel-2", result.ChannelIds);
        Assert.Single(result.ChannelIds);
    }

    /// <summary>
    ///     Verifies that reducing a UserLeftChannel event for last channel results in empty set.
    /// </summary>
    [Fact]
    [AllureStep("Reduce UserLeftChannel last channel")]
    public void ReduceUserLeftChannelLastChannel()
    {
        // Arrange
        UserLeftChannelReducer reducer = new();
        UserLeftChannel evt = new()
        {
            ChannelId = "channel-1",
            LeftAt = DateTimeOffset.UtcNow,
        };
        UserState state = new()
        {
            IsRegistered = true,
            UserId = "user-123",
            DisplayName = "John",
            ChannelIds = ImmutableHashSet.Create("channel-1"),
        };

        // Act
        UserState result = reducer.Reduce(state, evt);

        // Assert
        Assert.Empty(result.ChannelIds);
    }
}
