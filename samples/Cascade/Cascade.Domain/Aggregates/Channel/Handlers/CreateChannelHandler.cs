using System;
using System.Collections.Generic;

using Cascade.Domain.Aggregates.Channel.Commands;
using Cascade.Domain.Aggregates.Channel.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.Aggregates.Channel.Handlers;

/// <summary>
///     Handles the <see cref="CreateChannel" /> command.
/// </summary>
internal sealed class CreateChannelHandler : CommandHandlerBase<CreateChannel, ChannelAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        CreateChannel command,
        ChannelAggregate? state
    )
    {
        if (string.IsNullOrWhiteSpace(command.ChannelId))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Channel ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Channel name is required.");
        }

        if (string.IsNullOrWhiteSpace(command.CreatedBy))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Created by user ID is required.");
        }

        if (state is { IsCreated: true })
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Channel already exists.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
        [
            new ChannelCreated
            {
                ChannelId = command.ChannelId,
                Name = command.Name,
                CreatedBy = command.CreatedBy,
                CreatedAt = DateTimeOffset.UtcNow,
            },
        ]);
    }
}