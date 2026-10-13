using System;

using Mississippi.Tributary.Abstractions;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Reducers;

/// <summary>Removes the recorded notification and ordering entry while retaining its creation receipt.</summary>
internal sealed class NotificationDeletedReducer : EventReducerBase<NotificationDeleted, UserNotificationsAggregate>
{
    /// <inheritdoc />
    protected override UserNotificationsAggregate ReduceCore(
        UserNotificationsAggregate state,
        NotificationDeleted eventData
    ) =>
        state with
        {
            Notifications = state.Notifications.Remove(eventData.NotificationId),
            NotificationOrder = state.NotificationOrder.Remove(eventData.NotificationId, StringComparer.Ordinal),
        };
}