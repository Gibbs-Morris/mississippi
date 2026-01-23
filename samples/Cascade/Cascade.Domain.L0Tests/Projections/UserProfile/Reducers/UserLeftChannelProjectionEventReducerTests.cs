using System;
using System.Collections.Immutable;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.Aggregates.User.Events;
using Cascade.Domain.Projections.UserProfile;
using Cascade.Domain.Projections.UserProfile.Reducers;


namespace Cascade.Domain.L0Tests.Projections.UserProfile.Reducers;

/// <summary>
///     Unit tests for the <see cref="UserLeftChannelProjectionEventReducer" /> class.
/// </summary>
[AllureParentSuite("Cascade")]
[AllureSuite("Core")]
[AllureSubSuite("Unit")]
public sealed class UserLeftChannelProjectionEventReducerTests
{
    /// <summary>
    ///     Verifies that reducing a UserLeftChannel event decrements channel count.
    /// </summary>
    [Fact]
    [AllureStep("Reduce UserLeftChannel decrements channel count")]
    public void ReduceDecrementsChannelCount()
    {
        // Arrange
        UserLeftChannelProjectionEventReducer eventReducer = new();
        UserProfileProjection existingState = new()
        {
            UserId = "user-123",
            DisplayName = "John Doe",
            ChannelCount = 3,
            ChannelIds = ImmutableList.Create("channel-1", "channel-2", "channel-3"),
        };
        UserLeftChannel evt = new()
        {
            ChannelId = "channel-2",
            LeftAt = DateTimeOffset.UtcNow,
        };

        // Act
        UserProfileProjection result = eventReducer.Reduce(existingState, evt);

        // Assert
        Assert.Equal(2, result.ChannelCount);
        Assert.Equal(2, result.ChannelIds.Count);
        Assert.DoesNotContain("channel-2", result.ChannelIds);
    }

    /// <summary>
    ///     Verifies that reducing a UserLeftChannel event for non-existent channel is idempotent.
    /// </summary>
    [Fact]
    [AllureStep("Reduce UserLeftChannel ignores non-existent channel")]
    public void ReduceIgnoresNonExistentChannel()
    {
        // Arrange
        UserLeftChannelProjectionEventReducer eventReducer = new();
        UserProfileProjection existingState = new()
        {
            UserId = "user-123",
            DisplayName = "John Doe",
            ChannelCount = 2,
            ChannelIds = ImmutableList.Create("channel-1", "channel-2"),
        };
        UserLeftChannel evt = new()
        {
            ChannelId = "channel-3",
            LeftAt = DateTimeOffset.UtcNow,
        };

        // Act
        UserProfileProjection result = eventReducer.Reduce(existingState, evt);

        // Assert - Returns new instance but with same values
        Assert.NotSame(existingState, result);
        Assert.Equal(2, result.ChannelCount);
        Assert.Equal(2, result.ChannelIds.Count);
    }

    /// <summary>
    ///     Verifies that reducing a UserLeftChannel event removes the channel.
    /// </summary>
    [Fact]
    [AllureStep("Reduce UserLeftChannel removes channel")]
    public void ReduceRemovesChannel()
    {
        // Arrange
        UserLeftChannelProjectionEventReducer eventReducer = new();
        UserProfileProjection existingState = new()
        {
            UserId = "user-123",
            DisplayName = "John Doe",
            ChannelCount = 2,
            ChannelIds = ImmutableList.Create("channel-1", "channel-2"),
        };
        UserLeftChannel evt = new()
        {
            ChannelId = "channel-1",
            LeftAt = DateTimeOffset.UtcNow,
        };

        // Act
        UserProfileProjection result = eventReducer.Reduce(existingState, evt);

        // Assert
        Assert.NotSame(existingState, result);
        Assert.Equal(1, result.ChannelCount);
        Assert.Single(result.ChannelIds);
        Assert.DoesNotContain("channel-1", result.ChannelIds);
        Assert.Contains("channel-2", result.ChannelIds);
    }
}