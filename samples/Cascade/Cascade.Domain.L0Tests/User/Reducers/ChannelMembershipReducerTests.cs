using System;
using System.Collections.Immutable;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.Aggregates.User;
using Cascade.Domain.Aggregates.User.Events;
using Cascade.Domain.Aggregates.User.Reducers;


namespace Cascade.Domain.L0Tests.User.Reducers;

/// <summary>
///     Tests for <see cref="UserJoinedChannelEventReducer" /> and <see cref="UserLeftChannelEventReducer" />.
/// </summary>
[AllureParentSuite("Cascade")]
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
        UserJoinedChannelEventReducer eventReducer = new();
        UserJoinedChannel evt = new()
        {
            ChannelId = "channel-1",
            ChannelName = "General",
            JoinedAt = DateTimeOffset.UtcNow,
        };
        UserAggregate state = new()
        {
            IsRegistered = true,
            UserId = "user-123",
            DisplayName = "John",
            ChannelIds = ImmutableHashSet<string>.Empty,
        };

        // Act
        UserAggregate result = eventReducer.Reduce(state, evt);

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
        UserJoinedChannelEventReducer eventReducer = new();
        UserJoinedChannel evt = new()
        {
            ChannelId = "channel-2",
            ChannelName = "Random",
            JoinedAt = DateTimeOffset.UtcNow,
        };
        UserAggregate state = new()
        {
            IsRegistered = true,
            UserId = "user-123",
            DisplayName = "John",
            ChannelIds = ImmutableHashSet.Create("channel-1"),
        };

        // Act
        UserAggregate result = eventReducer.Reduce(state, evt);

        // Assert
        Assert.Contains("channel-1", result.ChannelIds);
        Assert.Contains("channel-2", result.ChannelIds);
        Assert.Equal(2, result.ChannelIds.Count);
    }

    /// <summary>
    ///     Verifies that reducing a UserLeftChannel event for last channel results in empty set.
    /// </summary>
    [Fact]
    [AllureStep("Reduce UserLeftChannel last channel")]
    public void ReduceUserLeftChannelLastChannel()
    {
        // Arrange
        UserLeftChannelEventReducer eventReducer = new();
        UserLeftChannel evt = new()
        {
            ChannelId = "channel-1",
            LeftAt = DateTimeOffset.UtcNow,
        };
        UserAggregate state = new()
        {
            IsRegistered = true,
            UserId = "user-123",
            DisplayName = "John",
            ChannelIds = ImmutableHashSet.Create("channel-1"),
        };

        // Act
        UserAggregate result = eventReducer.Reduce(state, evt);

        // Assert
        Assert.Empty(result.ChannelIds);
    }

    /// <summary>
    ///     Verifies that reducing a UserLeftChannel event removes the channel.
    /// </summary>
    [Fact]
    [AllureStep("Reduce UserLeftChannel removes channel")]
    public void ReduceUserLeftChannelRemovesChannel()
    {
        // Arrange
        UserLeftChannelEventReducer eventReducer = new();
        UserLeftChannel evt = new()
        {
            ChannelId = "channel-1",
            LeftAt = DateTimeOffset.UtcNow,
        };
        UserAggregate state = new()
        {
            IsRegistered = true,
            UserId = "user-123",
            DisplayName = "John",
            ChannelIds = ImmutableHashSet.Create("channel-1", "channel-2"),
        };

        // Act
        UserAggregate result = eventReducer.Reduce(state, evt);

        // Assert
        Assert.DoesNotContain("channel-1", result.ChannelIds);
        Assert.Contains("channel-2", result.ChannelIds);
        Assert.Single(result.ChannelIds);
    }
}