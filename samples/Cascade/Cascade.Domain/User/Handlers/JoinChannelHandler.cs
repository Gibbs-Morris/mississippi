// <copyright file="JoinChannelHandler.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

using Cascade.Domain.User.Commands;
using Cascade.Domain.User.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.User.Handlers;

/// <summary>
///     Command handler for recording that a user joined a channel.
/// </summary>
internal sealed class JoinChannelHandler : CommandHandler<JoinChannel, UserState>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        JoinChannel command,
        UserState? state
    )
    {
        if (state?.IsRegistered != true)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "User must be registered before joining a channel.");
        }

        if (string.IsNullOrWhiteSpace(command.ChannelId))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Channel ID is required.");
        }

        if (state.ChannelIds.Contains(command.ChannelId))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "User is already a member of this channel.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new UserJoinedChannel
                {
                    ChannelId = command.ChannelId,
                    JoinedAt = DateTimeOffset.UtcNow,
                },
            });
    }
}
