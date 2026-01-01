// <copyright file="RenameChannelHandler.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System.Collections.Generic;

using Cascade.Domain.Channel.Commands;
using Cascade.Domain.Channel.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.Channel.Handlers;

/// <summary>
///     Handles the <see cref="RenameChannel" /> command.
/// </summary>
internal sealed class RenameChannelHandler : CommandHandler<RenameChannel, ChannelState>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        RenameChannel command,
        ChannelState? state
    )
    {
        if (string.IsNullOrWhiteSpace(command.NewName))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "New name is required.");
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
                "Cannot rename an archived channel.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
        [
            new ChannelRenamed
            {
                OldName = state.Name,
                NewName = command.NewName,
            },
        ]);
    }
}