using Mississippi.Tributary.Abstractions;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Reducers;

/// <summary>
///     Replays the recorded deletion set without consulting time or reselecting entries.
/// </summary>
internal sealed class NotificationsDeletedReducer : EventReducerBase<NotificationsDeleted, UserNotificationsAggregate>
{
    /// <inheritdoc />
    protected override UserNotificationsAggregate ReduceCore(
        UserNotificationsAggregate state,
        NotificationsDeleted eventData
    ) =>
        state with
        {
            Notifications = state.Notifications.RemoveRange(eventData.NotificationIds),
        };
}