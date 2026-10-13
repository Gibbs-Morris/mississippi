using System;
using System.Collections.Immutable;
using System.Linq;

using Mississippi.Tributary.Abstractions;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Reducers;

/// <summary>Replays creation, recorded evictions, and receipt pruning without host policy or clocks.</summary>
internal sealed class NotificationCreatedReducer : EventReducerBase<NotificationCreated, UserNotificationsAggregate>
{
    /// <inheritdoc />
    protected override UserNotificationsAggregate ReduceCore(
        UserNotificationsAggregate state,
        NotificationCreated eventData
    )
    {
        UserNotificationsAggregate current = state ?? new();
        ImmutableHashSet<string> evicted = eventData.EvictedNotificationIds.ToImmutableHashSet(StringComparer.Ordinal);
        ImmutableDictionary<string, UserNotification> notifications = current.Notifications.RemoveRange(evicted)
            .Add(eventData.Notification.NotificationId, eventData.Notification);
        DateTimeOffset cutoff =
            current.ExpiredRequestsThrough is { } previous && (previous > eventData.ExpiredRequestsThrough)
                ? previous
                : eventData.ExpiredRequestsThrough;
        ImmutableDictionary<string, NotificationCreationReceipt> receipts = current.CreationReceipts.RemoveRange(
            current.CreationReceipts.Where(pair =>
                    (pair.Value.ExpiresAt <= cutoff) && !notifications.ContainsKey(pair.Value.NotificationId))
                .Select(pair => pair.Key));
        receipts = receipts.Add(
            eventData.CreationKey,
            new()
            {
                NotificationId = eventData.Notification.NotificationId,
                ExpiresAt = eventData.ExpiresAt,
                RequestFingerprint = eventData.RequestFingerprint,
            });
        return current with
        {
            Notifications = notifications,
            NotificationOrder = current.NotificationOrder.Where(id => !evicted.Contains(id))
                .Append(eventData.Notification.NotificationId)
                .ToImmutableArray(),
            CreationReceipts = receipts,
            ExpiredRequestsThrough = cutoff,
        };
    }
}