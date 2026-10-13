using System;
using System.Collections.Immutable;
using System.Linq;

using Mississippi.Tributary.Abstractions;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Reducers;

/// <summary>Replays the recorded deletion set and ordering changes while retaining creation receipts.</summary>
internal sealed class NotificationsDeletedReducer : EventReducerBase<NotificationsDeleted, UserNotificationsAggregate>
{
    /// <inheritdoc />
    protected override UserNotificationsAggregate ReduceCore(
        UserNotificationsAggregate state,
        NotificationsDeleted eventData
    )
    {
        ImmutableHashSet<string> deleted = eventData.NotificationIds.ToImmutableHashSet(StringComparer.Ordinal);
        return state with
        {
            Notifications = state.Notifications.RemoveRange(deleted),
            NotificationOrder = state.NotificationOrder.Where(id => !deleted.Contains(id)).ToImmutableArray(),
        };
    }
}