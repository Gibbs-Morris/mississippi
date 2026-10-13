using Mississippi.Tributary.Abstractions;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Reducers;

/// <summary>
///     Applies a recorded read timestamp while preserving notification content.
/// </summary>
internal sealed class NotificationMarkedAsReadReducer
    : EventReducerBase<NotificationMarkedAsRead, UserNotificationsAggregate>
{
    /// <inheritdoc />
    protected override UserNotificationsAggregate ReduceCore(
        UserNotificationsAggregate state,
        NotificationMarkedAsRead eventData
    ) =>
        state with
        {
            Notifications = state.Notifications.SetItem(
                eventData.NotificationId,
                state.Notifications[eventData.NotificationId] with
                {
                    ReadAt = eventData.ReadAt,
                }),
        };
}