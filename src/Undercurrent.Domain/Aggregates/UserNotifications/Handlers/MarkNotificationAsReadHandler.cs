using System;
using System.Collections.Generic;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications.Commands;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Handlers;

/// <summary>
///     Records the first read of an existing notification.
/// </summary>
internal sealed class MarkNotificationAsReadHandler
    : CommandHandlerBase<MarkNotificationAsRead, UserNotificationsAggregate>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MarkNotificationAsReadHandler" /> class.
    /// </summary>
    /// <param name="timeProvider">The provider of server UTC timestamps.</param>
    public MarkNotificationAsReadHandler(
        TimeProvider timeProvider
    ) =>
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    private TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        MarkNotificationAsRead command,
        UserNotificationsAggregate? state
    )
    {
        if (string.IsNullOrWhiteSpace(command.NotificationId))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Notification ID is required.");
        }

        if (state is null ||
            !state.Notifications.TryGetValue(command.NotificationId, out UserNotification? notification))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Notification does not exist.");
        }

        if (notification.ReadAt.HasValue)
        {
            return OperationResult.Ok<IReadOnlyList<object>>([]);
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
        [
            new NotificationMarkedAsRead
            {
                NotificationId = command.NotificationId,
                ReadAt = TimeProvider.GetUtcNow().ToUniversalTime(),
            },
        ]);
    }
}