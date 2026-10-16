using Mississippi.Tributary.Abstractions;
using Mississippi.Undercurrent.Abstractions.Projections.UserNotificationCounts;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;


namespace Mississippi.Undercurrent.Domain.Projections.UserNotificationCounts.Reducers;

/// <summary>Counts a creation and its recorded capacity removals in one transition.</summary>
internal sealed class NotificationCreatedProjectionReducer
    : EventReducerBase<NotificationCreated, UserNotificationCountsProjection>
{
    /// <inheritdoc />
    protected override UserNotificationCountsProjection ReduceCore(
        UserNotificationCountsProjection state,
        NotificationCreated eventData
    ) =>
        NotificationCounts.Update(
            state,
            state.ReadStates.RemoveRange(eventData.EvictedNotificationIds)
                .Add(eventData.Notification.NotificationId, eventData.Notification.ReadAt.HasValue));
}