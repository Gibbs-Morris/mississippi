using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications.Commands;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Handlers;

/// <summary>
///     Selects notifications before a caller-supplied instant and records a deterministic deletion fact.
/// </summary>
internal sealed class DeleteNotificationsBeforeHandler
    : CommandHandlerBase<DeleteNotificationsBefore, UserNotificationsAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        DeleteNotificationsBefore command,
        UserNotificationsAggregate? state
    )
    {
        if (state is null || state.Notifications.IsEmpty)
        {
            return OperationResult.Ok<IReadOnlyList<object>>([]);
        }

        DateTimeOffset cutoff = command.Before.ToUniversalTime();
        ImmutableArray<string> notificationIds = state.Notifications.Values
            .Where(notification => notification.CreatedAt < cutoff)
            .Select(notification => notification.NotificationId)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        if (notificationIds.IsEmpty)
        {
            return OperationResult.Ok<IReadOnlyList<object>>([]);
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
        [
            new NotificationsDeleted
            {
                NotificationIds = notificationIds,
                OlderThan = cutoff,
            },
        ]);
    }
}