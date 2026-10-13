using System;

using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Reducers;


namespace MississippiTests.Undercurrent.Domain.L0Tests.Aggregates.UserNotifications.Reducers;

/// <summary>
///     Verifies notification event replay and immutable inbox state through xUnit discovery.
/// </summary>
public sealed class NotificationCreatedReducerTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);

    private readonly NotificationCreatedReducer reducer = new();

    private static NotificationCreated CreateEvent(
        string notificationId
    ) =>
        UserNotificationsFixture.CreateEvent(notificationId) with
        {
            Notification = new()
            {
                NotificationId = notificationId,
                Title = " Deployment ready ",
                Description = "The deployment is ready to inspect.",
                IconKey = "deployment",
                NavigationTarget = "/deployments/42",
                CreatedAt = CreatedAt,
            },
        };

    /// <summary>
    ///     A new notification retains all previous notifications and leaves prior state unchanged.
    /// </summary>
    [Fact]
    public void AddingNotificationPreservesPreviousStateAndEntries()
    {
        NotificationCreated first = CreateEvent("first");
        NotificationCreated second = CreateEvent("second");
        UserNotificationsAggregate initial = new();
        UserNotificationsAggregate previous = reducer.Reduce(initial, first);
        UserNotificationsAggregate result = reducer.Reduce(previous, second);
        Assert.Empty(initial.Notifications);
        Assert.Equal(first.Notification, Assert.Single(previous.Notifications).Value);
        Assert.NotSame(previous, result);
        Assert.Equal(2, result.Notifications.Count);
        Assert.Equal(first.Notification, result.Notifications["first"]);
        Assert.Equal(second.Notification, result.Notifications["second"]);
    }

    /// <summary>Replaying a lower recorded cutoff preserves the prior expiry watermark.</summary>
    [Fact]
    public void ExpiryWatermarkNeverMovesBackwards()
    {
        UserNotificationsAggregate previous = reducer.Reduce(new(), UserNotificationsFixture.CreateEvent("first"));
        NotificationCreated olderCutoff = UserNotificationsFixture.CreateEvent("second") with
        {
            ExpiredRequestsThrough = UserNotificationsFixture.CreatedAt.AddTicks(-1),
        };
        UserNotificationsAggregate result = reducer.Reduce(previous, olderCutoff);
        Assert.Equal(previous.ExpiredRequestsThrough, result.ExpiredRequestsThrough);
        Assert.Equal(2, result.Notifications.Count);
    }

    /// <summary>
    ///     The first event builds an inbox and preserves the exact notification content.
    /// </summary>
    [Fact]
    public void FirstEventCreatesInboxWithExactContent()
    {
        NotificationCreated created = CreateEvent(" external|notification ");
        UserNotificationsAggregate result = reducer.Reduce(null!, created);
        Assert.Equal(created.Notification, Assert.Single(result.Notifications).Value);
        Assert.Equal(created.Notification, result.Notifications[" external|notification "]);
    }

    /// <summary>
    ///     Updating one inbox leaves a separate inbox unchanged.
    /// </summary>
    [Fact]
    public void IndependentInboxesDoNotShareNotifications()
    {
        UserNotificationsAggregate other = reducer.Reduce(new(), CreateEvent("other"));
        UserNotificationsAggregate result = reducer.Reduce(new(), CreateEvent("current"));
        Assert.Equal("other", Assert.Single(other.Notifications).Key);
        Assert.Equal("current", Assert.Single(result.Notifications).Key);
        Assert.False(other.Notifications.ContainsKey("current"));
        Assert.False(result.Notifications.ContainsKey("other"));
    }

    /// <summary>
    ///     Notification identities preserve case rather than merging distinct values.
    /// </summary>
    [Fact]
    public void NotificationIdsAreCaseSensitive()
    {
        UserNotificationsAggregate initial = reducer.Reduce(new(), CreateEvent("Notice"));
        UserNotificationsAggregate result = reducer.Reduce(initial, CreateEvent("notice"));
        Assert.Equal(2, result.Notifications.Count);
        Assert.Equal("Notice", result.Notifications["Notice"].NotificationId);
        Assert.Equal("notice", result.Notifications["notice"].NotificationId);
    }

    /// <summary>
    ///     A notification can omit every optional presentation field.
    /// </summary>
    [Fact]
    public void OptionalMetadataCanBeAbsent()
    {
        UserNotification notification = new()
        {
            NotificationId = "minimal",
            Title = "Ready",
            CreatedAt = CreatedAt,
        };
        UserNotificationsAggregate result = reducer.Reduce(
            new(),
            UserNotificationsFixture.CreateEvent("minimal") with
            {
                Notification = notification,
            });
        UserNotification stored = Assert.Single(result.Notifications).Value;
        Assert.Equal(notification, stored);
        Assert.Null(stored.Description);
        Assert.Null(stored.IconKey);
        Assert.Null(stored.NavigationTarget);
    }
}