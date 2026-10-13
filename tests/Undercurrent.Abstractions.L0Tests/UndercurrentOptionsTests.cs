using System;

using Mississippi.Undercurrent.Abstractions;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;


namespace MississippiTests.Undercurrent.Abstractions.L0Tests;

/// <summary>Verifies the public host policy defaults and independent configuration objects.</summary>
public sealed class UndercurrentOptionsTests
{
    /// <summary>Hosts receive the agreed bounded inbox, receipt, lifetime, and field defaults.</summary>
    [Fact]
    public void DefaultsMatchTheNotificationContract()
    {
        UserNotificationsOptions options = new UndercurrentOptions().Notifications;
        Assert.Equal(1_000, options.MaxNotifications);
        Assert.Equal(10_000, options.MaxCreationReceipts);
        Assert.Equal(TimeSpan.FromDays(7), options.MaxRequestLifetime);
        Assert.Equal(200, options.MaxTitleLength);
        Assert.Equal(2_000, options.MaxDescriptionLength);
        Assert.Equal(128, options.MaxIconKeyLength);
        Assert.Equal(2_048, options.MaxNavigationTargetLength);
        Assert.Equal(256, options.MaxCreationKeyLength);
        Assert.True(typeof(UndercurrentOptions).IsPublic);
        Assert.True(typeof(UserNotificationsOptions).IsPublic);
    }

    /// <summary>Configuring one host does not mutate defaults for another host.</summary>
    [Fact]
    public void OptionsInstancesHaveIndependentNotificationPolicies()
    {
        UndercurrentOptions first = new();
        UndercurrentOptions second = new();
        first.Notifications.MaxNotifications = 3;
        Assert.Equal(1_000, second.Notifications.MaxNotifications);
        Assert.NotSame(first.Notifications, second.Notifications);
    }
}