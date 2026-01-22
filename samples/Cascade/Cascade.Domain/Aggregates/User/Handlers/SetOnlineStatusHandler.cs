using System;
using System.Collections.Generic;

using Cascade.Domain.Aggregates.User.Commands;
using Cascade.Domain.Aggregates.User.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.Aggregates.User.Handlers;

/// <summary>
///     Command handler for setting a user's online status.
/// </summary>
internal sealed class SetOnlineStatusHandler : CommandHandlerBase<SetOnlineStatus, UserAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        SetOnlineStatus command,
        UserAggregate? state
    )
    {
        if (state?.IsRegistered != true)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "User must be registered before setting online status.");
        }

        if (command.IsOnline == state.IsOnline)
        {
            // No change needed - return empty event list (no-op)
            return OperationResult.Ok<IReadOnlyList<object>>([]);
        }

        object @event = command.IsOnline
            ? new UserWentOnline
            {
                Timestamp = DateTimeOffset.UtcNow,
            }
            : new UserWentOffline
            {
                Timestamp = DateTimeOffset.UtcNow,
            };
        return OperationResult.Ok<IReadOnlyList<object>>(new[] { @event });
    }
}