using Mississippi.Tributary.Abstractions;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Reducers;

/// <summary>
///     Adds a created notification without changing earlier inbox state.
/// </summary>
internal sealed class NotificationCreatedReducer : EventReducerBase<NotificationCreated, UserNotificationsAggregate>
{
    /// <inheritdoc />
    protected override UserNotificationsAggregate ReduceCore(
        UserNotificationsAggregate state,
        NotificationCreated eventData
    )
    {
        UserNotificationsAggregate current = state ?? new();
        return current with
        {
            Notifications = current.Notifications.Add(eventData.Notification.NotificationId, eventData.Notification),
        };
    }
}