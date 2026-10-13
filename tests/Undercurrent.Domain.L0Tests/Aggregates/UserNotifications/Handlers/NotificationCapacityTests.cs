using System;
using System.Collections.Generic;
using System.Globalization;

using Microsoft.Extensions.Time.Testing;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications.Commands;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Handlers;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Reducers;


namespace MississippiTests.Undercurrent.Domain.L0Tests.Aggregates.UserNotifications.Handlers;

/// <summary>Verifies rolling capacity, deletion receipts, deadline pruning, and clock rollback.</summary>
public sealed class NotificationCapacityTests
{
    private readonly FakeTimeProvider timeProvider = new(UserNotificationsFixture.CreatedAt);

    private static NotificationCreated Apply(
        CreateNotificationHandler handler,
        CreateNotification command,
        ref UserNotificationsAggregate state
    )
    {
        OperationResult<IReadOnlyList<object>> result = handler.Handle(command, state);
        Assert.True(result.Success, result.ErrorMessage);
        NotificationCreated created = Assert.IsType<NotificationCreated>(Assert.Single(result.Value));
        state = new NotificationCreatedReducer().Reduce(state, created);
        return created;
    }

    private CreateNotificationHandler Handler(
        int notifications = 1,
        int receipts = 4
    ) =>
        UserNotificationsFixture.CreateHandler(
            timeProvider,
            new()
            {
                MaxNotifications = notifications,
                MaxCreationReceipts = receipts,
            });

    /// <summary>An expired receipt stays available while its notification is still stored.</summary>
    [Fact]
    public void ActiveReceiptSurvivesItsExpiry()
    {
        UserNotificationsAggregate state = new();
        CreateNotificationHandler handler = Handler(2);
        CreateNotification original = UserNotificationsFixture.CreateCommand("active") with
        {
            ExpiresAt = UserNotificationsFixture.CreatedAt.AddMinutes(1),
        };
        Apply(handler, original, ref state);
        timeProvider.Advance(TimeSpan.FromMinutes(1));
        Apply(handler, UserNotificationsFixture.CreateCommand("next"), ref state);
        Assert.True(state.CreationReceipts.ContainsKey("active"));
        UserNotificationsFixture.AssertSuccessfulNoOp(handler, original, state);
    }

    /// <summary>Capacity removes the oldest entry even when it is unread and a newer entry is read.</summary>
    /// <param name="oldestRead">Whether the oldest notification has been read.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CapacityEvictsOldestRegardlessOfReadState(
        bool oldestRead
    )
    {
        CreateNotificationHandler handler = Handler(2, 6);
        UserNotificationsAggregate state = new();
        NotificationCreated first = Apply(handler, UserNotificationsFixture.CreateCommand("first"), ref state);
        NotificationCreated second = Apply(handler, UserNotificationsFixture.CreateCommand("second"), ref state);
        state = new NotificationMarkedAsReadReducer().Reduce(
            state,
            new()
            {
                NotificationId = oldestRead ? first.Notification.NotificationId : second.Notification.NotificationId,
                ReadAt = timeProvider.GetUtcNow(),
            });
        UserNotificationsAggregate previous = state;
        NotificationCreated third = Apply(handler, UserNotificationsFixture.CreateCommand("third"), ref state);
        Assert.Equal([first.Notification.NotificationId], third.EvictedNotificationIds);
        Assert.Equal([second.Notification.NotificationId, third.Notification.NotificationId], state.NotificationOrder);
        Assert.False(state.Notifications.ContainsKey(first.Notification.NotificationId));
        Assert.Equal(2, state.Notifications.Count);
        Assert.Equal(3, state.CreationReceipts.Count);
        Assert.Equal(2, previous.Notifications.Count);
        Assert.True(previous.Notifications.ContainsKey(first.Notification.NotificationId));
    }

    /// <summary>Old read and delete retries cannot target a subsequently generated identity.</summary>
    [Fact]
    public void DelayedReadAndDeleteLeaveNewNotificationUntouched()
    {
        UserNotificationsAggregate state = new();
        CreateNotificationHandler handler = Handler();
        NotificationCreated old = Apply(handler, UserNotificationsFixture.CreateCommand("old"), ref state);
        state = new NotificationDeletedReducer().Reduce(
            state,
            new()
            {
                NotificationId = old.Notification.NotificationId,
            });
        NotificationCreated current = Apply(handler, UserNotificationsFixture.CreateCommand("new"), ref state);
        OperationResult<IReadOnlyList<object>> read = new MarkNotificationAsReadHandler(timeProvider).Handle(
            new()
            {
                NotificationId = old.Notification.NotificationId,
            },
            state);
        Assert.False(read.Success);
        UserNotificationsFixture.AssertSuccessfulNoOp(
            new DeleteNotificationHandler(),
            new()
            {
                NotificationId = old.Notification.NotificationId,
            },
            state);
        Assert.Equal(current.Notification, Assert.Single(state.Notifications).Value);
        Assert.Null(Assert.Single(state.Notifications).Value.ReadAt);
    }

    /// <summary>Deleted, retained requests return the agreed terminal error for every removal path.</summary>
    /// <param name="removal">The removal path.</param>
    [Theory]
    [InlineData("delete")]
    [InlineData("cleanup")]
    [InlineData("eviction")]
    public void DeletedCreationRetryReturnsAlreadyDeleted(
        string removal
    )
    {
        UserNotificationsAggregate state = new();
        CreateNotificationHandler handler = Handler();
        CreateNotification request = UserNotificationsFixture.CreateCommand();
        NotificationCreated original = Apply(handler, request, ref state);
        if (removal == "delete")
        {
            state = new NotificationDeletedReducer().Reduce(
                state,
                new()
                {
                    NotificationId = original.Notification.NotificationId,
                });
        }
        else if (removal == "cleanup")
        {
            OperationResult<IReadOnlyList<object>> cleanup = new DeleteNotificationsBeforeHandler().Handle(
                new()
                {
                    Before = original.Notification.CreatedAt.AddTicks(1),
                },
                state);
            NotificationsDeleted deleted = Assert.IsType<NotificationsDeleted>(Assert.Single(cleanup.Value!));
            state = new NotificationsDeletedReducer().Reduce(state, deleted);
        }
        else
        {
            Apply(handler, UserNotificationsFixture.CreateCommand("new"), ref state);
        }

        OperationResult<IReadOnlyList<object>> retry = handler.Handle(request, state);
        Assert.False(retry.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, retry.ErrorCode);
        Assert.Equal("Notification has already been deleted.", retry.ErrorMessage);
        Assert.Null(retry.Value);
        Assert.False(state.Notifications.ContainsKey(original.Notification.NotificationId));
        Assert.True(state.CreationReceipts.ContainsKey(request.CreationKey));
    }

    /// <summary>Expired entries evicted in this creation can release their receipt slots in the same transition.</summary>
    [Fact]
    public void EvictionAndExpiryCanReleaseReceiptCapacity()
    {
        UserNotificationsAggregate state = new();
        CreateNotificationHandler handler = Handler(1, 2);
        CreateNotification first = UserNotificationsFixture.CreateCommand("first") with
        {
            ExpiresAt = UserNotificationsFixture.CreatedAt.AddMinutes(1),
        };
        Apply(handler, first, ref state);
        Apply(
            handler,
            first with
            {
                CreationKey = "second",
            },
            ref state);
        timeProvider.Advance(TimeSpan.FromMinutes(1));
        Apply(handler, UserNotificationsFixture.CreateCommand("third"), ref state);
        Assert.Equal("third", Assert.Single(state.CreationReceipts).Key);
        Assert.Single(state.Notifications);
    }

    /// <summary>Expired deleted keys are pruned at the exact deadline and cannot resurrect.</summary>
    [Fact]
    public void ExpiredDeletedReceiptIsPrunedAndRetryStaysExpired()
    {
        UserNotificationsAggregate state = new();
        CreateNotificationHandler handler = Handler();
        CreateNotification original = UserNotificationsFixture.CreateCommand("old") with
        {
            ExpiresAt = UserNotificationsFixture.CreatedAt.AddMinutes(1),
        };
        NotificationCreated old = Apply(handler, original, ref state);
        state = new NotificationDeletedReducer().Reduce(
            state,
            new()
            {
                NotificationId = old.Notification.NotificationId,
            });
        timeProvider.Advance(TimeSpan.FromMinutes(1));
        NotificationCreated next = Apply(handler, UserNotificationsFixture.CreateCommand("next"), ref state);
        Assert.False(state.CreationReceipts.ContainsKey("old"));
        Assert.Equal(timeProvider.GetUtcNow(), state.ExpiredRequestsThrough);
        OperationResult<IReadOnlyList<object>> retry = handler.Handle(original, state);
        Assert.False(retry.Success);
        Assert.Equal("Creation request has expired.", retry.ErrorMessage);
        Assert.Equal(next.Notification.NotificationId, Assert.Single(state.Notifications).Key);
        FakeTimeProvider earlierClock = new(UserNotificationsFixture.CreatedAt);
        OperationResult<IReadOnlyList<object>> afterRollback =
            UserNotificationsFixture.CreateHandler(earlierClock).Handle(original, state);
        Assert.False(afterRollback.Success);
        Assert.Equal("Creation request has expired.", afterRollback.ErrorMessage);
        Assert.Null(afterRollback.Value);
    }

    /// <summary>Receipt pressure rejects creation without evicting the current notification.</summary>
    [Fact]
    public void FullUnexpiredReceiptHistoryRejectsCreationWithoutChanges()
    {
        UserNotificationsAggregate state = new();
        CreateNotificationHandler handler = Handler(1, 3);
        for (int index = 0; index < 3; index++)
        {
            Apply(
                handler,
                UserNotificationsFixture.CreateCommand(index.ToString(CultureInfo.InvariantCulture)),
                ref state);
        }

        string currentId = Assert.Single(state.Notifications).Key;
        OperationResult<IReadOnlyList<object>> result = handler.Handle(
            UserNotificationsFixture.CreateCommand("fourth"),
            state);
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
        Assert.Equal("Creation receipt capacity is exhausted.", result.ErrorMessage);
        Assert.Null(result.Value);
        Assert.Equal(currentId, Assert.Single(state.Notifications).Key);
        Assert.Equal(3, state.CreationReceipts.Count);
        UserNotificationsFixture.AssertSuccessfulNoOp(handler, UserNotificationsFixture.CreateCommand("2"), state);
    }

    /// <summary>Incomplete ordering data fails safely when the configured cap requires an eviction.</summary>
    [Fact]
    public void InconsistentOrderCannotBypassCapacity()
    {
        UserNotificationsAggregate state = new NotificationCreatedReducer().Reduce(
            new(),
            UserNotificationsFixture.CreateEvent());
        state = state with
        {
            NotificationOrder = [],
        };
        OperationResult<IReadOnlyList<object>> result =
            Handler().Handle(UserNotificationsFixture.CreateCommand("next"), state);
        Assert.False(result.Success);
        Assert.Equal("Notification order is inconsistent.", result.ErrorMessage);
        Assert.Null(result.Value);
        Assert.Single(state.Notifications);
    }

    /// <summary>Expiry pruning uses request deadlines rather than receipt insertion order.</summary>
    [Fact]
    public void ReceiptExpiryOrderCanDifferFromCreationOrder()
    {
        UserNotificationsAggregate state = new();
        CreateNotificationHandler handler = Handler();
        Apply(
            handler,
            UserNotificationsFixture.CreateCommand("long") with
            {
                ExpiresAt = UserNotificationsFixture.CreatedAt.AddHours(2),
            },
            ref state);
        NotificationCreated shortLived = Apply(
            handler,
            UserNotificationsFixture.CreateCommand("short") with
            {
                ExpiresAt = UserNotificationsFixture.CreatedAt.AddHours(1),
            },
            ref state);
        state = new NotificationDeletedReducer().Reduce(
            state,
            new()
            {
                NotificationId = shortLived.Notification.NotificationId,
            });
        timeProvider.Advance(TimeSpan.FromHours(1));
        Apply(handler, UserNotificationsFixture.CreateCommand("next"), ref state);
        Assert.True(state.CreationReceipts.ContainsKey("long"));
        Assert.False(state.CreationReceipts.ContainsKey("short"));
    }

    /// <summary>A lower host cap takes effect through one recorded transition rather than replay-time trimming.</summary>
    [Fact]
    public void ReducedCapRecordsAllNecessaryEvictions()
    {
        UserNotificationsAggregate state = new();
        CreateNotificationHandler original = Handler(3, 8);
        for (int index = 0; index < 3; index++)
        {
            Apply(
                original,
                UserNotificationsFixture.CreateCommand(index.ToString(CultureInfo.InvariantCulture)),
                ref state);
        }

        UserNotificationsAggregate previous = state;
        NotificationCreated created = Apply(Handler(1, 8), UserNotificationsFixture.CreateCommand("new"), ref state);
        Assert.Equal(previous.NotificationOrder, created.EvictedNotificationIds);
        Assert.Equal(created.Notification, Assert.Single(state.Notifications).Value);
        Assert.Equal(3, previous.Notifications.Count);
        UserNotificationsAggregate replayed = new NotificationCreatedReducer().Reduce(previous, created);
        Assert.Equal(state.NotificationOrder, replayed.NotificationOrder);
        Assert.Equal(state.Notifications.Keys, replayed.Notifications.Keys);
    }
}