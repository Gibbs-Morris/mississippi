using System;
using System.Collections.Immutable;
using System.Linq;

using Mississippi.Tributary.Abstractions;
using Mississippi.Undercurrent.Abstractions.Projections.UserNotificationList;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;


namespace Mississippi.Undercurrent.Domain.Projections.UserNotificationList.Reducers;

/// <summary>Applies the recorded read timestamp while preserving list order and presentation content.</summary>
internal sealed class NotificationMarkedAsReadProjectionReducer
    : EventReducerBase<NotificationMarkedAsRead, UserNotificationListProjection>
{
    /// <inheritdoc />
    protected override UserNotificationListProjection ReduceCore(
        UserNotificationListProjection state,
        NotificationMarkedAsRead eventData
    ) =>
        state with
        {
            Notifications = state.Notifications.Select(notification =>
                    string.Equals(notification.NotificationId, eventData.NotificationId, StringComparison.Ordinal)
                        ? notification with
                        {
                            ReadAt = eventData.ReadAt,
                        }
                        : notification)
                .ToImmutableArray(),
        };
}