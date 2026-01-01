// <copyright file="AddMemberHandler.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

using Cascade.Domain.Channel.Commands;
using Cascade.Domain.Channel.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.Channel.Handlers;

/// <summary>
///     Handles the <see cref="AddMember" /> command.
/// </summary>
internal sealed class AddMemberHandler : CommandHandler<AddMember, ChannelAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        AddMember command,
        ChannelAggregate? state
    )
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "User ID is required.");
        }

        if (state is not { IsCreated: true })
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Channel does not exist.");
        }

        if (state.IsArchived)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Cannot add members to an archived channel.");
        }

        if (state.MemberIds.Contains(command.UserId))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "User is already a member of the channel.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
        [
            new MemberAdded
            {
                UserId = command.UserId,
                AddedAt = DateTimeOffset.UtcNow,
            },
        ]);
    }
}