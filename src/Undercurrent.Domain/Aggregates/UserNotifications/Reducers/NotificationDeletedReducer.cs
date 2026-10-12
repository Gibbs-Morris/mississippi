using Mississippi.Tributary.Abstractions;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Reducers;

/// <summary>
///     Removes only the notification identified by the recorded deletion.
/// </summary>
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
        };
}