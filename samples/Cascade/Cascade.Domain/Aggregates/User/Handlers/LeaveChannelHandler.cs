using System;
using System.Collections.Generic;

using Cascade.Domain.Aggregates.User.Commands;
using Cascade.Domain.Aggregates.User.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.Aggregates.User.Handlers;

/// <summary>
///     Command handler for recording that a user left a channel.
/// </summary>
internal sealed class LeaveChannelHandler : CommandHandlerBase<LeaveChannel, UserAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        LeaveChannel command,
        UserAggregate? state
    )
    {
        if (state?.IsRegistered != true)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "User must be registered before leaving a channel.");
        }

        if (string.IsNullOrWhiteSpace(command.ChannelId))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Channel ID is required.");
        }

        if (!state.ChannelIds.Contains(command.ChannelId))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "User is not a member of this channel.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            new object[]
            {
                new UserLeftChannel
                {
                    ChannelId = command.ChannelId,
                    LeftAt = DateTimeOffset.UtcNow,
                },
            });
    }
}