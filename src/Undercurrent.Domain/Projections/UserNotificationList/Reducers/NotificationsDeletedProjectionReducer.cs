using System;
using System.Collections.Immutable;
using System.Linq;

using Mississippi.Tributary.Abstractions;
using Mississippi.Undercurrent.Abstractions.Projections.UserNotificationList;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;


namespace Mississippi.Undercurrent.Domain.Projections.UserNotificationList.Reducers;

/// <summary>Removes the recorded cleanup IDs without re-evaluating its historical cutoff.</summary>
internal sealed class NotificationsDeletedProjectionReducer
    : EventReducerBase<NotificationsDeleted, UserNotificationListProjection>
{
    /// <inheritdoc />
    protected override UserNotificationListProjection ReduceCore(
        UserNotificationListProjection state,
        NotificationsDeleted eventData
    )
    {
        ImmutableHashSet<string> removed = eventData.NotificationIds.ToImmutableHashSet(StringComparer.Ordinal);
        return state with
        {
            Notifications = state.Notifications.Where(notification => !removed.Contains(notification.NotificationId))
                .ToImmutableArray(),
        };
    }
}