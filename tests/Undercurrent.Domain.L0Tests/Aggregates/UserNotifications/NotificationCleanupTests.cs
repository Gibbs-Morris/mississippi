using System;
using System.Collections.Immutable;

using Microsoft.Extensions.Time.Testing;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.TestHarness.Aggregates;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Commands;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Handlers;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Reducers;


namespace MississippiTests.Undercurrent.Domain.L0Tests.Aggregates.UserNotifications;

/// <summary>
///     Exposes deterministic age-cleanup and replay scenarios for xUnit discovery.
/// </summary>
public sealed class NotificationCleanupTests
{
    private static readonly ImmutableArray<string> ExpectedDeletedIds = ["a-old", "z-old"];

    private readonly FakeTimeProvider timeProvider = new(UserNotificationsFixture.CreatedAt.AddDays(30));

    /// <summary>
    ///     Retrying the same cleanup after deletion emits nothing.
    /// </summary>
    [Fact]
    public void CleanupRetryProducesNoEvent()
    {
        UserNotificationsAggregate state = UserNotificationsFixture.CreateScenario(timeProvider)
            .Given(
                UserNotificationsFixture.CreateEvent(),
                new NotificationsDeleted
                {
                    NotificationIds = ["notice"],
                    OlderThan = timeProvider.GetUtcNow().AddDays(-7),
                })
            .State;
        UserNotificationsFixture.AssertSuccessfulNoOp(
            new DeleteNotificationsOlderThanHandler(timeProvider),
            new()
            {
                Days = 7,
            },
            state);
        Assert.Empty(state.Notifications);
    }

    /// <summary>
    ///     Cleanup removes older notifications, regardless of read status, while retaining exact-boundary and newer entries.
    /// </summary>
    /// <param name="days">The nonnegative age threshold, including zero.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(30)]
    public void CleanupUsesExclusiveUtcBoundaryAndStableIds(
        int days
    )
    {
        timeProvider.SetUtcNow(timeProvider.GetUtcNow().ToOffset(TimeSpan.FromHours(2)));
        DateTimeOffset cutoff = timeProvider.GetUtcNow().ToUniversalTime().AddDays(-days);
        NotificationCreated boundary = UserNotificationsFixture.CreateEvent("boundary", cutoff);
        NotificationCreated future = UserNotificationsFixture.CreateEvent(
            "future",
            timeProvider.GetUtcNow().AddDays(1));
        UserNotificationsFixture.CreateScenario(timeProvider)
            .Given(
                UserNotificationsFixture.CreateEvent("z-old", cutoff.AddTicks(-1)),
                UserNotificationsFixture.CreateEvent("a-old", cutoff.AddDays(-1)),
                new NotificationMarkedAsRead
                {
                    NotificationId = "a-old",
                    ReadAt = cutoff,
                },
                boundary,
                future)
            .When(
                new DeleteNotificationsOlderThan
                {
                    Days = days,
                })
            .ThenSucceeds()
            .ThenEmitsEvents(item =>
            {
                NotificationsDeleted deleted = Assert.IsType<NotificationsDeleted>(item);
                Assert.Equal(ExpectedDeletedIds, deleted.NotificationIds);
                Assert.Equal(cutoff, deleted.OlderThan);
                Assert.Equal(TimeSpan.Zero, deleted.OlderThan.Offset);
            })
            .ThenState(state =>
            {
                Assert.Equal(2, state.Notifications.Count);
                Assert.Equal(boundary.Notification, state.Notifications["boundary"]);
                Assert.Equal(future.Notification, state.Notifications["future"]);
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
            new DeleteNotificationsOlderThanHandler(timeProvider),
            new()
            {
                Days = 7,
            },
            withoutHistory ? null : new UserNotificationsAggregate());

    /// <summary>
    ///     Negative ages fail validation even when the inbox has no history.
    /// </summary>
    [Fact]
    public void NegativeAgeIsInvalid() =>
        new DeleteNotificationsOlderThanHandler(timeProvider).ShouldFailWithMessage(
            null,
            new()
            {
                Days = -1,
            },
            AggregateErrorCodes.InvalidCommand,
            "Age in days must not be negative.");

    /// <summary>
    ///     Cleanup with nothing older than the cutoff emits no event and retains the inbox.
    /// </summary>
    [Fact]
    public void NoMatchingNotificationsProduceNoEvent()
    {
        NotificationCreated existing = UserNotificationsFixture.CreateEvent(
            "boundary",
            timeProvider.GetUtcNow().AddDays(-7));
        UserNotificationsAggregate state = UserNotificationsFixture.CreateScenario(timeProvider).Given(existing).State;
        UserNotificationsFixture.AssertSuccessfulNoOp(
            new DeleteNotificationsOlderThanHandler(timeProvider),
            new()
            {
                Days = 7,
            },
            state);
        Assert.Equal(existing.Notification, Assert.Single(state.Notifications).Value);
    }

    /// <summary>
    ///     Cleanup handlers require an explicit time provider.
    /// </summary>
    [Fact]
    public void NullTimeProviderIsRejected() =>
        Assert.Throws<ArgumentNullException>(() => new DeleteNotificationsOlderThanHandler(null!));

    /// <summary>
    ///     Replay deletes only the recorded IDs and does not re-evaluate age or change prior state.
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

    /// <summary>
    ///     A positive age beyond the supported date range is a no-op rather than an overflow.
    /// </summary>
    [Fact]
    public void VeryLargeAgePreservesInboxWithoutOverflow()
    {
        NotificationCreated oldest = UserNotificationsFixture.CreateEvent("oldest", DateTimeOffset.MinValue);
        UserNotificationsAggregate state = UserNotificationsFixture.CreateScenario(timeProvider).Given(oldest).State;
        UserNotificationsFixture.AssertSuccessfulNoOp(
            new DeleteNotificationsOlderThanHandler(timeProvider),
            new()
            {
                Days = int.MaxValue,
            },
            state);
        Assert.Equal(oldest.Notification, Assert.Single(state.Notifications).Value);
    }
}
