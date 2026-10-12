using System;
using System.Collections.Generic;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Commands;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Handlers;

/// <summary>
///     Validates creation requests and recognizes retries before producing a notification event.
/// </summary>
internal sealed class CreateNotificationHandler : CommandHandlerBase<CreateNotification, UserNotificationsAggregate>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CreateNotificationHandler" /> class.
    /// </summary>
    /// <param name="timeProvider">The provider of server UTC timestamps.</param>
    public CreateNotificationHandler(
        TimeProvider timeProvider
    ) =>
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    private TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        CreateNotification command,
        UserNotificationsAggregate? state
    )
    {
        if (string.IsNullOrWhiteSpace(command.NotificationId))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Notification ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Title))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Title is required.");
        }

        if (state is not null &&
            state.Notifications.TryGetValue(command.NotificationId, out UserNotification? existing))
        {
            if (string.Equals(existing.Title, command.Title, StringComparison.Ordinal) &&
                string.Equals(existing.Description, command.Description, StringComparison.Ordinal) &&
                string.Equals(existing.IconKey, command.IconKey, StringComparison.Ordinal) &&
                string.Equals(existing.TargetUrl, command.TargetUrl, StringComparison.Ordinal))
            {
                return OperationResult.Ok<IReadOnlyList<object>>([]);
            }

            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Notification ID already identifies different content.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
        [
            new NotificationCreated
            {
                Notification = new()
                {
                    NotificationId = command.NotificationId,
                    Title = command.Title,
                    Description = command.Description,
                    IconKey = command.IconKey,
                    TargetUrl = command.TargetUrl,
                    CreatedAt = TimeProvider.GetUtcNow().ToUniversalTime(),
                },
            },
        ]);
    }
}