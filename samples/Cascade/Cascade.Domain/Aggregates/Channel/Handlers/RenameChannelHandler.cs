using System.Collections.Generic;

using Cascade.Domain.Aggregates.Channel.Commands;
using Cascade.Domain.Aggregates.Channel.Events;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Cascade.Domain.Aggregates.Channel.Handlers;

/// <summary>
///     Handles the <see cref="RenameChannel" /> command.
/// </summary>
internal sealed class RenameChannelHandler : CommandHandlerBase<RenameChannel, ChannelAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        RenameChannel command,
        ChannelAggregate? state
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