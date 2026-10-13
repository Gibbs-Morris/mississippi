using System;
using System.Collections.Immutable;
using System.Linq;

using Mississippi.Tributary.Abstractions;
using Mississippi.Undercurrent.Abstractions.Projections.UserNotificationList;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;


namespace Mississippi.Undercurrent.Domain.Projections.UserNotificationList.Reducers;

/// <summary>Removes only the exact recorded notification ID.</summary>
internal sealed class NotificationDeletedProjectionReducer
    : EventReducerBase<NotificationDeleted, UserNotificationListProjection>
{
    /// <inheritdoc />
    protected override UserNotificationListProjection ReduceCore(
        UserNotificationListProjection state,
        NotificationDeleted eventData
    ) =>
        state with
        {
            Notifications = state.Notifications.Where(notification => !string.Equals(
                    notification.NotificationId,
                    eventData.NotificationId,
                    StringComparison.Ordinal))
                .ToImmutableArray(),
        };
}