using Mississippi.Tributary.Abstractions;
using Mississippi.Undercurrent.Abstractions.Projections.UserNotificationCounts;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;


namespace Mississippi.Undercurrent.Domain.Projections.UserNotificationCounts.Reducers;

/// <summary>Removes the recorded notification from total and unread counts.</summary>
internal sealed class NotificationDeletedProjectionReducer
    : EventReducerBase<NotificationDeleted, UserNotificationCountsProjection>
{
    /// <inheritdoc />
    protected override UserNotificationCountsProjection ReduceCore(
        UserNotificationCountsProjection state,
        NotificationDeleted eventData
    ) =>
        NotificationCounts.Update(state, state.ReadStates.Remove(eventData.NotificationId));
}