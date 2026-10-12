using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Commands;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Events;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications.Handlers;

/// <summary>
///     Selects old notifications using server time and records a deterministic deletion fact.
/// </summary>
internal sealed class DeleteNotificationsOlderThanHandler
    : CommandHandlerBase<DeleteNotificationsOlderThan, UserNotificationsAggregate>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DeleteNotificationsOlderThanHandler" /> class.
    /// </summary>
    /// <param name="timeProvider">The provider of server UTC timestamps.</param>
    public DeleteNotificationsOlderThanHandler(
        TimeProvider timeProvider
    ) =>
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    private TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        DeleteNotificationsOlderThan command,
        UserNotificationsAggregate? state
    )
    {
        if (command.Days < 0)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Age in days must not be negative.");
        }

        if (state is null || state.Notifications.IsEmpty)
        {
            return OperationResult.Ok<IReadOnlyList<object>>([]);
        }

        DateTimeOffset now = TimeProvider.GetUtcNow().ToUniversalTime();
        if (command.Days > (now - DateTimeOffset.MinValue).Days)
        {
            return OperationResult.Ok<IReadOnlyList<object>>([]);
        }

        DateTimeOffset cutoff = now.AddDays(-command.Days);
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