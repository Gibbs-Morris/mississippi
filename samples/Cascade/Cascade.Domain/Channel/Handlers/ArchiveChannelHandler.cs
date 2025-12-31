// <copyright file="ArchiveChannelHandler.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

using Cascade.Domain.Channel.Commands;
using Cascade.Domain.Channel.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.Channel.Handlers;

/// <summary>
///     Handles the <see cref="ArchiveChannel" /> command.
/// </summary>
internal sealed class ArchiveChannelHandler : CommandHandler<ArchiveChannel, ChannelState>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        ArchiveChannel command,
        ChannelState? state
    )
    {
        if (string.IsNullOrWhiteSpace(command.ArchivedBy))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Archived by user ID is required.");
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
                "Channel is already archived.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
        [
            new ChannelArchived
            {
                ArchivedBy = command.ArchivedBy,
                ArchivedAt = DateTimeOffset.UtcNow,
            },
        ]);
    }
}
