using System;
using System.Collections.Immutable;
using System.Linq;

using Mississippi.Tributary.Abstractions;
using Mississippi.Undercurrent.Abstractions.Projections.UserNotificationList;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;


namespace Mississippi.Undercurrent.Domain.Projections.UserNotificationList.Reducers;

/// <summary>Prepends recorded creation content and removes the recorded capacity evictions.</summary>
internal sealed class NotificationCreatedProjectionReducer
    : EventReducerBase<NotificationCreated, UserNotificationListProjection>
{
    /// <inheritdoc />
    protected override UserNotificationListProjection ReduceCore(
        UserNotificationListProjection state,
        NotificationCreated eventData
    )
    {
        ImmutableHashSet<string> removed = eventData.EvictedNotificationIds.ToImmutableHashSet(StringComparer.Ordinal);
        return state with
        {
            Notifications = ImmutableArray.Create(eventData.Notification)
                .AddRange(state.Notifications.Where(notification => !removed.Contains(notification.NotificationId))),
        };
    }
}