using Mississippi.Tributary.Abstractions;
using Mississippi.Undercurrent.Abstractions.Projections.UserNotificationCounts;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;


namespace Mississippi.Undercurrent.Domain.Projections.UserNotificationCounts.Reducers;

/// <summary>Counts only retained notifications after the recorded cleanup set is removed.</summary>
internal sealed class NotificationsDeletedProjectionReducer
    : EventReducerBase<NotificationsDeleted, UserNotificationCountsProjection>
{
    /// <inheritdoc />
    protected override UserNotificationCountsProjection ReduceCore(
        UserNotificationCountsProjection state,
        NotificationsDeleted eventData
    ) =>
        NotificationCounts.Update(state, state.ReadStates.RemoveRange(eventData.NotificationIds));
}