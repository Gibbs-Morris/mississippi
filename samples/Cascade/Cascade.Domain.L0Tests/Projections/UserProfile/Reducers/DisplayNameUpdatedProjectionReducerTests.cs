using System;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.Projections.UserProfile;
using Cascade.Domain.Projections.UserProfile.Reducers;
using Cascade.Domain.User.Events;


namespace Cascade.Domain.L0Tests.Projections.UserProfile.Reducers;

/// <summary>
///     Unit tests for the <see cref="DisplayNameUpdatedProjectionReducer" /> class.
/// </summary>
[AllureParentSuite("Cascade")]
[AllureSuite("Core")]
[AllureSubSuite("Unit")]
public sealed class DisplayNameUpdatedProjectionReducerTests
{
    /// <summary>
    ///     Verifies that reducing a DisplayNameUpdated event updates the display name.
    /// </summary>
    [Fact]
    [AllureStep("Reduce DisplayNameUpdated updates display name")]
    public void ReduceUpdatesDisplayName()
    {
        // Arrange
        DisplayNameUpdatedProjectionReducer reducer = new();
        UserProfileProjection existingState = new()
        {
            UserId = "user-123",
            DisplayName = "Old Name",
            IsOnline = true,
            RegisteredAt = DateTimeOffset.UtcNow.AddDays(-1),
            ChannelCount = 2,
        };
        DisplayNameUpdated evt = new()
        {
            OldDisplayName = "Old Name",
            NewDisplayName = "New Name",
        };

        // Act
        UserProfileProjection result = reducer.Reduce(existingState, evt);

        // Assert
        Assert.NotSame(existingState, result);
        Assert.Equal("New Name", result.DisplayName);
        Assert.Equal("user-123", result.UserId);
        Assert.True(result.IsOnline);
        Assert.Equal(2, result.ChannelCount);
    }
}