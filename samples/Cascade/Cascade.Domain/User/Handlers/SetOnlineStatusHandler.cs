// <copyright file="SetOnlineStatusHandler.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

using Cascade.Domain.User.Commands;
using Cascade.Domain.User.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.User.Handlers;

/// <summary>
///     Command handler for setting a user's online status.
/// </summary>
internal sealed class SetOnlineStatusHandler : CommandHandler<SetOnlineStatus, UserState>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        SetOnlineStatus command,
        UserState? state
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
            ? new UserWentOnline { Timestamp = DateTimeOffset.UtcNow }
            : new UserWentOffline { Timestamp = DateTimeOffset.UtcNow };

        return OperationResult.Ok<IReadOnlyList<object>>(new[] { @event });
    }
}
