using System.Collections.Immutable;

using Mississippi.Tributary.Abstractions;
using Mississippi.Undercurrent.Abstractions.Projections.UserNotificationCounts;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;


namespace Mississippi.Undercurrent.Domain.Projections.UserNotificationCounts.Reducers;

/// <summary>Updates unread counts without introducing absent or removed notification IDs.</summary>
internal sealed class NotificationMarkedAsReadProjectionReducer
    : EventReducerBase<NotificationMarkedAsRead, UserNotificationCountsProjection>
{
    /// <inheritdoc />
    protected override UserNotificationCountsProjection ReduceCore(
        UserNotificationCountsProjection state,
        NotificationMarkedAsRead eventData
    )
    {
        ImmutableDictionary<string, bool> readStates = state.ReadStates.ContainsKey(eventData.NotificationId)
            ? state.ReadStates.SetItem(eventData.NotificationId, true)
            : state.ReadStates;
        return NotificationCounts.Update(state, readStates);
    }
}