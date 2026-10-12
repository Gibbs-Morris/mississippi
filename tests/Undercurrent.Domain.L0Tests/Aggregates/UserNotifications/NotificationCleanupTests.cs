using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.Extensions.Time.Testing;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Commands;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Handlers;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Reducers;


namespace MississippiTests.Undercurrent.Domain.L0Tests.Aggregates.UserNotifications;

/// <summary>
///     Exposes deterministic timestamp-cutoff cleanup and replay scenarios for xUnit discovery.
/// </summary>
public sealed class NotificationCleanupTests
{
    private static readonly ImmutableArray<string> ExpectedDeletedIds = ["a-old", "z-old"];

    private readonly FakeTimeProvider timeProvider = new(UserNotificationsFixture.CreatedAt.AddDays(30));

    /// <summary>
    ///     The same caller cutoff selects the same notifications when server time changes.
    /// </summary>
    [Fact]
    public void CallerCutoffIsIndependentOfServerTime()
    {
        DateTimeOffset cutoff = UserNotificationsFixture.CreatedAt.AddMinutes(1).AddTicks(1);
        UserNotificationsAggregate state = UserNotificationsFixture.CreateScenario(timeProvider)
            .Given(
                UserNotificationsFixture.CreateEvent("old", cutoff.AddTicks(-1)),
                UserNotificationsFixture.CreateEvent("boundary", cutoff),
                UserNotificationsFixture.CreateEvent("later", cutoff.AddTicks(1)))
            .State;
        DeleteNotificationsBefore command = new()
        {
            Before = cutoff,
        };
        DeleteNotificationsBeforeHandler handler = new();
        OperationResult<IReadOnlyList<object>> firstResult = handler.Handle(command, state);
        Assert.True(firstResult.Success);
        NotificationsDeleted first =
            Assert.IsType<NotificationsDeleted>(Assert.Single(firstResult.Value));
        timeProvider.Advance(TimeSpan.FromDays(365));
        OperationResult<IReadOnlyList<object>> secondResult = handler.Handle(command, state);
        Assert.True(secondResult.Success);
        NotificationsDeleted second =
            Assert.IsType<NotificationsDeleted>(Assert.Single(secondResult.Value));
        Assert.Equal("old", Assert.Single(first.NotificationIds));
        Assert.Equal(first.NotificationIds, second.NotificationIds);
        Assert.Equal(cutoff, first.OlderThan);
        Assert.Equal(first.OlderThan, second.OlderThan);
        Assert.Equal(3, state.Notifications.Count);
    }

    /// <summary>
    ///     Retrying the same cleanup after deletion emits nothing.
    /// </summary>
    [Fact]
    public void CleanupRetryProducesNoEvent()
    {
        DateTimeOffset cutoff = UserNotificationsFixture.CreatedAt.AddMinutes(1);
        UserNotificationsAggregate state = UserNotificationsFixture.CreateScenario(timeProvider)
            .Given(
                UserNotificationsFixture.CreateEvent(),
                new NotificationsDeleted
                {
                    NotificationIds = ["notice"],
                    OlderThan = cutoff,
                })
            .State;
        UserNotificationsFixture.AssertSuccessfulNoOp(
            new DeleteNotificationsBeforeHandler(),
            new()
            {
                Before = cutoff,
            },
            state);
        Assert.Empty(state.Notifications);
    }

    /// <summary>
    ///     Cleanup compares instants at tick precision and preserves entries exactly at or after the cutoff.
    /// </summary>
    /// <param name="offsetMinutes">The caller's UTC offset in minutes.</param>
    [Theory]
    [InlineData(-840)]
    [InlineData(-330)]
    [InlineData(0)]
    [InlineData(345)]
    [InlineData(840)]
    public void CleanupUsesExclusiveInstantBoundaryAndStableIds(
        int offsetMinutes
    )
    {
        DateTimeOffset cutoff = UserNotificationsFixture.CreatedAt.AddMinutes(17)
            .AddTicks(1234)
            .ToOffset(TimeSpan.FromMinutes(offsetMinutes));
        NotificationCreated boundary = UserNotificationsFixture.CreateEvent("boundary", cutoff.ToUniversalTime());
        NotificationCreated later = UserNotificationsFixture.CreateEvent(
            "later",
            cutoff.AddTicks(1).ToOffset(TimeSpan.FromHours(-4)));
        UserNotificationsFixture.CreateScenario(timeProvider)
            .Given(
                UserNotificationsFixture.CreateEvent("z-old", cutoff.AddTicks(-1)),
                UserNotificationsFixture.CreateEvent("a-old", cutoff.AddMinutes(-1)),
                new NotificationMarkedAsRead
                {
                    NotificationId = "a-old",
                    ReadAt = cutoff,
                },
                boundary,
                later)
            .When(
                new DeleteNotificationsBefore
                {
                    Before = cutoff,
                })
            .ThenSucceeds()
            .ThenEmitsEvents(item =>
            {
                NotificationsDeleted deleted = Assert.IsType<NotificationsDeleted>(item);
                Assert.Equal(ExpectedDeletedIds, deleted.NotificationIds);
                Assert.Equal(cutoff.ToUniversalTime(), deleted.OlderThan);
                Assert.Equal(TimeSpan.Zero, deleted.OlderThan.Offset);
            })
            .ThenState(state =>
            {
                Assert.Equal(2, state.Notifications.Count);
                Assert.Equal(boundary.Notification, state.Notifications["boundary"]);
                Assert.Equal(later.Notification, state.Notifications["later"]);
            });
    }

    /// <summary>
    ///     Null and empty inboxes produce no deletion event.
    /// </summary>
    /// <param name="withoutHistory">Whether the aggregate has no state yet.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EmptyInboxCleanupIsHarmless(
        bool withoutHistory
    ) =>
        UserNotificationsFixture.AssertSuccessfulNoOp(
            new DeleteNotificationsBeforeHandler(),
            new()
            {
                Before = UserNotificationsFixture.CreatedAt,
            },
            withoutHistory ? null : new UserNotificationsAggregate());

    /// <summary>
    ///     The maximum supported cutoff removes earlier entries while preserving the exact boundary.
    /// </summary>
    [Fact]
    public void MaximumCutoffPreservesExactBoundary()
    {
        NotificationCreated latest = UserNotificationsFixture.CreateEvent("latest", DateTimeOffset.MaxValue);
        UserNotificationsFixture.CreateScenario(timeProvider)
            .Given(UserNotificationsFixture.CreateEvent("earlier", DateTimeOffset.MaxValue.AddTicks(-1)), latest)
            .When(
                new DeleteNotificationsBefore
                {
                    Before = DateTimeOffset.MaxValue,
                })
            .ThenSucceeds()
            .ThenEmitsEvents(item =>
            {
                NotificationsDeleted deleted = Assert.IsType<NotificationsDeleted>(item);
                Assert.Equal("earlier", Assert.Single(deleted.NotificationIds));
                Assert.Equal(DateTimeOffset.MaxValue, deleted.OlderThan);
            })
            .ThenState(state => Assert.Equal(latest.Notification, Assert.Single(state.Notifications).Value));
    }

    /// <summary>
    ///     The minimum supported cutoff is valid and has no earlier notifications to delete.
    /// </summary>
    [Fact]
    public void MinimumCutoffProducesNoEvent()
    {
        NotificationCreated oldest = UserNotificationsFixture.CreateEvent("oldest", DateTimeOffset.MinValue);
        UserNotificationsAggregate state = UserNotificationsFixture.CreateScenario(timeProvider).Given(oldest).State;
        UserNotificationsFixture.AssertSuccessfulNoOp(
            new DeleteNotificationsBeforeHandler(),
            new()
            {
                Before = DateTimeOffset.MinValue,
            },
            state);
        Assert.Equal(oldest.Notification, Assert.Single(state.Notifications).Value);
    }

    /// <summary>
    ///     Cleanup with nothing before the cutoff emits no event and retains the inbox.
    /// </summary>
    [Fact]
    public void NoMatchingNotificationsProduceNoEvent()
    {
        NotificationCreated existing = UserNotificationsFixture.CreateEvent("boundary");
        UserNotificationsAggregate state = UserNotificationsFixture.CreateScenario(timeProvider).Given(existing).State;
        UserNotificationsFixture.AssertSuccessfulNoOp(
            new DeleteNotificationsBeforeHandler(),
            new()
            {
                Before = existing.Notification.CreatedAt,
            },
            state);
        Assert.Equal(existing.Notification, Assert.Single(state.Notifications).Value);
    }

    /// <summary>
    ///     Replay deletes only the recorded IDs without reselecting entries or changing prior state.
    /// </summary>
    [Fact]
    public void ReplayUsesRecordedIdsAndPreservesPreviousState()
    {
        NotificationCreated selected = UserNotificationsFixture.CreateEvent("selected");
        NotificationCreated retained = UserNotificationsFixture.CreateEvent("retained", DateTimeOffset.MinValue);
        UserNotificationsAggregate previous =
            UserNotificationsFixture.CreateScenario(timeProvider).Given(selected, retained).State;
        NotificationsDeleted deleted = new()
        {
            NotificationIds = ["selected"],
            OlderThan = DateTimeOffset.MinValue,
        };
        timeProvider.Advance(TimeSpan.FromDays(365));
        UserNotificationsAggregate result = new NotificationsDeletedReducer().Reduce(previous, deleted);
        Assert.Equal(2, previous.Notifications.Count);
        Assert.NotSame(previous, result);
        Assert.Equal(retained.Notification, Assert.Single(result.Notifications).Value);
    }
}
