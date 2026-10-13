using System.Collections.Generic;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications.Commands;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Handlers;

/// <summary>
///     Deletes existing notifications and accepts retries after deletion.
/// </summary>
internal sealed class DeleteNotificationHandler : CommandHandlerBase<DeleteNotification, UserNotificationsAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        DeleteNotification command,
        UserNotificationsAggregate? state
    )
    {
        if (string.IsNullOrWhiteSpace(command.NotificationId))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Notification ID is required.");
        }

        if (state is null || !state.Notifications.ContainsKey(command.NotificationId))
        {
            return OperationResult.Ok<IReadOnlyList<object>>([]);
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
        [
            new NotificationDeleted
            {
                NotificationId = command.NotificationId,
            },
        ]);
    }
}