using System;
using System.Collections.Generic;

using Microsoft.Extensions.Time.Testing;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.TestHarness.Aggregates;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Commands;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Handlers;


namespace MississippiTests.Undercurrent.Domain.L0Tests.Aggregates.UserNotifications;

/// <summary>
///     Exposes notification read and deletion scenarios for xUnit discovery.
/// </summary>
public sealed class NotificationLifecycleTests
{
    private readonly FakeTimeProvider timeProvider = new(UserNotificationsFixture.CreatedAt.AddDays(2));

    /// <summary>
    ///     A create retry leaves a notification's read state intact.
    /// </summary>
    [Fact]
    public void CreateRetryPreservesReadState()
    {
        NotificationCreated existing = UserNotificationsFixture.CreateEvent();
        DateTimeOffset firstRead = UserNotificationsFixture.CreatedAt.AddDays(1);
        UserNotificationsAggregate state = UserNotificationsFixture.CreateScenario(timeProvider)
            .Given(
                existing,
                new NotificationMarkedAsRead
                {
                    NotificationId = "notice",
                    ReadAt = firstRead,
                })
            .State;
        UserNotificationsFixture.AssertSuccessfulNoOp(
            new CreateNotificationHandler(timeProvider),
            new()
            {
                NotificationId = existing.Notification.NotificationId,
                Title = existing.Notification.Title,
                Description = existing.Notification.Description,
                IconKey = existing.Notification.IconKey,
                TargetUrl = existing.Notification.TargetUrl,
            },
            state);
        Assert.Equal(
            existing.Notification with
            {
                ReadAt = firstRead,
            },
            Assert.Single(state.Notifications).Value);
    }

    /// <summary>
    ///     Deletion removes only the exact ID and leaves the other notification unchanged.
    /// </summary>
    [Fact]
    public void DeleteTargetsOnlySpecifiedNotification()
    {
        NotificationCreated other = UserNotificationsFixture.CreateEvent("Notice");
        UserNotificationsFixture.CreateScenario(timeProvider)
            .Given(UserNotificationsFixture.CreateEvent(), other)
            .When(
                new DeleteNotification
                {
                    NotificationId = "notice",
                })
            .ThenSucceeds()
            .ThenEmitsEvents(item => Assert.Equal("notice", Assert.IsType<NotificationDeleted>(item).NotificationId))
            .ThenState(state => Assert.Equal(other.Notification, Assert.Single(state.Notifications).Value));
    }

    /// <summary>
    ///     Repeating a deletion succeeds without another event.
    /// </summary>
    [Fact]
    public void DeletionRetryEmitsNothing()
    {
        UserNotificationsAggregate state = UserNotificationsFixture.CreateScenario(timeProvider)
            .Given(
                UserNotificationsFixture.CreateEvent(),
                new NotificationDeleted
                {
                    NotificationId = "notice",
                })
            .State;
        UserNotificationsFixture.AssertSuccessfulNoOp(
            new DeleteNotificationHandler(),
            new()
            {
                NotificationId = "notice",
            },
            state);
        Assert.Empty(state.Notifications);
    }

    /// <summary>
    ///     Deleting from an inbox with no history succeeds without an event.
    /// </summary>
    [Fact]
    public void DeletionWithoutHistoryIsHarmless() =>
        UserNotificationsFixture.AssertSuccessfulNoOp(
            new DeleteNotificationHandler(),
            new()
            {
                NotificationId = "notice",
            },
            null);

    /// <summary>
    ///     Both operations validate IDs before applying state-dependent behavior.
    /// </summary>
    /// <param name="notificationId">The invalid ID.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public void InvalidIdsFailBothOperations(
        string? notificationId
    )
    {
        new DeleteNotificationHandler().ShouldFailWithMessage(
            null,
            new()
            {
                NotificationId = notificationId!,
            },
            AggregateErrorCodes.InvalidCommand,
            "Notification ID is required.");
        new MarkNotificationAsReadHandler(timeProvider).ShouldFailWithMessage(
            null,
            new()
            {
                NotificationId = notificationId!,
            },
            AggregateErrorCodes.InvalidCommand,
            "Notification ID is required.");
    }

    /// <summary>
    ///     Marking as read updates only the target and records server UTC time.
    /// </summary>
    [Fact]
    public void MarkAsReadPreservesContentAndOtherNotifications()
    {
        NotificationCreated existing = UserNotificationsFixture.CreateEvent();
        NotificationCreated other = UserNotificationsFixture.CreateEvent("Notice");
        timeProvider.SetUtcNow(timeProvider.GetUtcNow().ToOffset(TimeSpan.FromHours(2)));
        DateTimeOffset readAt = timeProvider.GetUtcNow().ToUniversalTime();
        UserNotificationsFixture.CreateScenario(timeProvider)
            .Given(existing, other)
            .When(
                new MarkNotificationAsRead
                {
                    NotificationId = "notice",
                })
            .ThenSucceeds()
            .ThenEmitsEvents(item => Assert.Equal(
                new()
                {
                    NotificationId = "notice",
                    ReadAt = readAt,
                },
                Assert.IsType<NotificationMarkedAsRead>(item)))
            .ThenState(state =>
            {
                Assert.Equal(2, state.Notifications.Count);
                Assert.Equal(
                    existing.Notification with
                    {
                        ReadAt = readAt,
                    },
                    state.Notifications["notice"]);
                Assert.Equal(other.Notification, state.Notifications["Notice"]);
                Assert.Null(existing.Notification.ReadAt);
                Assert.Equal(TimeSpan.Zero, state.Notifications["notice"].ReadAt.GetValueOrDefault().Offset);
            });
    }

    /// <summary>
    ///     An absent or differently cased ID cannot delete another notification.
    /// </summary>
    /// <param name="notificationId">The absent ID to delete.</param>
    [Theory]
    [InlineData("missing")]
    [InlineData("notice")]
    public void MissingDeletionPreservesExistingNotification(
        string notificationId
    )
    {
        NotificationCreated existing = UserNotificationsFixture.CreateEvent("Notice");
        UserNotificationsAggregate state = UserNotificationsFixture.CreateScenario(timeProvider).Given(existing).State;
        UserNotificationsFixture.AssertSuccessfulNoOp(
            new DeleteNotificationHandler(),
            new()
            {
                NotificationId = notificationId,
            },
            state);
        Assert.Equal(existing.Notification, Assert.Single(state.Notifications).Value);
    }

    /// <summary>
    ///     Missing notifications cannot be marked as read.
    /// </summary>
    /// <param name="withoutHistory">Whether the inbox has no state.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MissingReadReturnsInvalidState(
        bool withoutHistory
    )
    {
        OperationResult<IReadOnlyList<object>> result = new MarkNotificationAsReadHandler(timeProvider).Handle(
            new()
            {
                NotificationId = "notice",
            },
            withoutHistory ? null : new UserNotificationsAggregate());
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
        Assert.Equal("Notification does not exist.", result.ErrorMessage);
        Assert.Null(result.Value);
    }

    /// <summary>
    ///     Read handlers require an explicit time provider.
    /// </summary>
    [Fact]
    public void NullTimeProviderIsRejected() =>
        Assert.Throws<ArgumentNullException>(() => new MarkNotificationAsReadHandler(null!));

    /// <summary>
    ///     Retrying a read preserves the first read time without another event.
    /// </summary>
    [Fact]
    public void ReadRetryPreservesFirstReadTimestamp()
    {
        DateTimeOffset firstRead = UserNotificationsFixture.CreatedAt.AddDays(1);
        UserNotificationsAggregate state = UserNotificationsFixture.CreateScenario(timeProvider)
            .Given(
                UserNotificationsFixture.CreateEvent(),
                new NotificationMarkedAsRead
                {
                    NotificationId = "notice",
                    ReadAt = firstRead,
                })
            .State;
        UserNotificationsFixture.AssertSuccessfulNoOp(
            new MarkNotificationAsReadHandler(timeProvider),
            new()
            {
                NotificationId = "notice",
            },
            state);
        Assert.Equal(firstRead, Assert.Single(state.Notifications).Value.ReadAt);
    }
}