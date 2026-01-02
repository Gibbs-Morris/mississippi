using System;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Cascade.Domain.User;
using Cascade.Domain.User.Events;
using Cascade.Domain.User.Reducers;


namespace Cascade.Domain.L0Tests.User.Reducers;

/// <summary>
///     Tests for <see cref="UserWentOnlineReducer" /> and <see cref="UserWentOfflineReducer" />.
/// </summary>
[AllureSuite("User")]
[AllureSubSuite("Reducers")]
[AllureFeature("OnlineStatus")]
public sealed class OnlineStatusReducerTests
{
    /// <summary>
    ///     Verifies that reducing a UserWentOffline event sets IsOnline to false.
    /// </summary>
    [Fact]
    [AllureStep("Reduce UserWentOffline sets offline")]
    public void ReduceUserWentOfflineSetsOffline()
    {
        // Arrange
        UserWentOfflineReducer reducer = new();
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        UserWentOffline evt = new()
        {
            Timestamp = timestamp,
        };
        UserAggregate state = new()
        {
            IsRegistered = true,
            UserId = "user-123",
            DisplayName = "John",
            IsOnline = true,
        };

        // Act
        UserAggregate result = reducer.Reduce(state, evt);

        // Assert
        Assert.False(result.IsOnline);
        Assert.Equal(timestamp, result.LastSeenAt);
        Assert.Equal("user-123", result.UserId);
    }

    /// <summary>
    ///     Verifies that reducing a UserWentOnline event sets IsOnline to true.
    /// </summary>
    [Fact]
    [AllureStep("Reduce UserWentOnline sets online")]
    public void ReduceUserWentOnlineSetsOnline()
    {
        // Arrange
        UserWentOnlineReducer reducer = new();
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        UserWentOnline evt = new()
        {
            Timestamp = timestamp,
        };
        UserAggregate state = new()
        {
            IsRegistered = true,
            UserId = "user-123",
            DisplayName = "John",
            IsOnline = false,
        };

        // Act
        UserAggregate result = reducer.Reduce(state, evt);

        // Assert
        Assert.True(result.IsOnline);
        Assert.Equal(timestamp, result.LastSeenAt);
        Assert.Equal("user-123", result.UserId);
    }
}