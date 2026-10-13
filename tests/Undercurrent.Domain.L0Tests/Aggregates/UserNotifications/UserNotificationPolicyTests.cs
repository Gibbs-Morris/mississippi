using System;

using Microsoft.Extensions.Options;

using Mississippi.Undercurrent.Abstractions;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications;


namespace MississippiTests.Undercurrent.Domain.L0Tests.Aggregates.UserNotifications;

/// <summary>Verifies that host options become an immutable policy snapshot.</summary>
public sealed class UserNotificationPolicyTests
{
    /// <summary>Later mutation of the options model cannot change the captured policy.</summary>
    [Fact]
    public void MutableOptionsCannotChangeCapturedPolicy()
    {
        UndercurrentOptions options = new();
        UserNotificationPolicy policy = new(Options.Create(options));
        options.Notifications.MaxNotifications = 2;
        options.Notifications.MaxCreationReceipts = 4;
        options.Notifications.MaxRequestLifetime = TimeSpan.FromHours(1);
        options.Notifications.MaxSnapshotSizeBytes = 100;
        options.Notifications.MaxCreationKeyLength = 1;
        options.Notifications.MaxTitleLength = 1;
        options.Notifications.MaxDescriptionLength = 0;
        options.Notifications.MaxIconKeyLength = 0;
        options.Notifications.MaxNavigationTargetLength = 0;
        Assert.Equal(1_000, policy.MaxNotifications);
        Assert.Equal(10_000, policy.MaxCreationReceipts);
        Assert.Equal(1_000_000, policy.MaxSnapshotSizeBytes);
        Assert.Equal(TimeSpan.FromDays(7), policy.MaxRequestLifetime);
        Assert.Equal(256, policy.MaxCreationKeyLength);
        Assert.Equal(200, policy.MaxTitleLength);
        Assert.Equal(2_000, policy.MaxDescriptionLength);
        Assert.Equal(128, policy.MaxIconKeyLength);
        Assert.Equal(2_048, policy.MaxNavigationTargetLength);
    }
}